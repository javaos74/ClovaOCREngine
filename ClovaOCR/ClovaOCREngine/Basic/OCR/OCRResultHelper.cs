using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UiPath.OCR.Contracts.DataContracts;
using UiPath.OCR.Contracts;

namespace ClovaOCRActivities.Basic.OCR
{
    internal static class OCRResultHelper
    {
        internal class RequestBody
        {
            public string version { get; set; } = "V2";
            public string requestId { get; set; } = Guid.NewGuid().ToString();
            public string timestamp { get; set; } = System.DateTime.Now.Ticks.ToString();
            public string lang { get; set; } = "ko"; // default 

            public List<RequestImage> images { get; set; } = new List<RequestImage>();

            public override string ToString()
            {
                return $"version={version}, requestId={requestId}, timestamp={timestamp}, lang={lang}, images[0].name={images.ToArray()[0].name} images[0].format={images.ToArray()[0].format}";
            }
        }
        internal class RequestImage 
        {
            public RequestImage( string n)
            {
                this.name = n;
            }
            public string name { get; set; }
            public string format { get; set; } = "png";

            public string[] templateIds { get; set; } = new string[0];
        }

        internal static  UiPath.OCR.Contracts.OCRRotation GetOCRRotation( Single rot)
        {
 #if DEBUG
            System.Console.WriteLine(" roation : " + rot);
 #endif
            if ( rot >= 88 && rot <= 92)
                return OCRRotation.Rotated90;
            else if ( rot >= 178 && rot <= 182)
                return OCRRotation.Rotated180;
            else if( rot >= 268 && rot <= 272)
                return OCRRotation.Rotated270;
            else if ( rot >= 358 || rot <= 2)
                return OCRRotation.None;
            else
                return OCRRotation.Other;
        }

        internal static async Task<OCRResult> FromClovaClient(string file_path, Dictionary<string, object> options)
        {
            OCRResult ocrResult = new OCRResult();
            var client = new UiPathHttpClient(options["endpoint"].ToString());
            client.setSecret(options["secret"].ToString());
            var reqBody = new RequestBody();
            reqBody.images.Add(new RequestImage(Path.GetFileNameWithoutExtension(file_path) + ".png"));
#if DEBUG
            Console.WriteLine(reqBody.ToString());
#endif
            reqBody.lang = options["lang"].ToString();
#if DEBUG
            Console.WriteLine(JsonConvert.SerializeObject(reqBody));
#endif

            client.AddField("message", JsonConvert.SerializeObject(reqBody));
            client.AddFile(file_path);

            var resp = await client.Upload();
#if DEBUG
            System.Console.WriteLine(resp.status + " == > " + (resp.body.Length > 100 ? resp.body.Substring(0, 100) : resp.body));
            System.IO.File.WriteAllText(@"C:\Temp\clova_resp.json", resp.body);
#endif
            if (resp.status == HttpStatusCode.OK)
            {
                StringBuilder sb = new StringBuilder();
                JObject respJson = JObject.Parse(resp.body);
                JArray blocks = (JArray)respJson["images"][0]["fields"];
                ocrResult.Words = blocks.Select(p => new Word
                {
                    Text = (string)p["inferText"],
                    Confidence = Convert.ToInt32(100 * ((double)p["inferConfidence"])),
                    PolygonPoints = ((JArray)p["boundingPoly"]["vertices"]).Select(v => new PointF
                    {
                        X = (float)v["x"],
                        Y = (float)v["y"]
                    }).ToArray(),
                    Characters = ((string)p["inferText"]).Select(ch => new Character
                    {
                        Char = ch,
                    }).ToArray()
                }).ToArray();
                foreach (var blk in blocks)
                {
                    sb.Append((string)blk["inferText"]);
                    sb.Append(((Boolean)blk["lineBreak"]) ? Environment.NewLine : " ");
                }
                foreach (var word in ocrResult.Words)
                {
                    var x = word.PolygonPoints[0].X;
                    var y = word.PolygonPoints[0].Y;
                    var w = Math.Abs(word.PolygonPoints[1].X - x);
                    var y2 = word.PolygonPoints[3].Y;

                    float dx = w / word.Characters.Length;
                    float dy = Math.Abs(y2 - y) / word.Characters.Length;
                    int idx = 0;
#if DEBUG
                   // System.Console.WriteLine(string.Format("{0} has {1} characters", word.Text, word.Characters.Length));
#endif
                    foreach (var c in word.Characters)
                    {
                        c.PolygonPoints = new[] { new PointF(x + dx * idx, y), new PointF(x + dx * (idx + 1), y), new PointF(x + dx * (idx + 1), y2), new PointF(x + dx * idx, y2) };
                        c.Confidence = word.Confidence;
                        idx++;
                    }
                }
                ocrResult.Text = sb.ToString();
                ocrResult.SkewAngle = 0;
                ocrResult.Confidence = 0;
            }
            return ocrResult;
        }


        internal static PointF[] reducePolygonPoints ( string word, int idx,  PointF [] points,  OCRRotation rot)
        {
            var x = points[0].X;
            var y = points[0].Y;
            var w = Math.Abs(points[1].X - x);
            var h = Math.Abs(points[1].Y - y);
            var y2 = points[3].Y;
            var x2 = points[3].X;

            float dx = w / word.Length;
            float dy = h / word.Length;

            if( rot == OCRRotation.Rotated90)
                return new[] { new PointF(x, y - dy * idx), new PointF(x, y - dy * (idx + 1)), new PointF(x2, y - dy * (idx + 1)), new PointF(x2, y - dy * idx) };
            else if ( rot == OCRRotation.Rotated270 )
                return new[] { new PointF(x, y + dy * idx), new PointF(x, y + dy * (idx + 1)), new PointF(x2, y + dy * (idx + 1)), new PointF(x2, y + dy * idx) };
            else if( rot == OCRRotation.Rotated180)
                return new[] { new PointF(x - dx * idx, y), new PointF(x - dx * (idx + 1), y), new PointF(x - dx * (idx + 1), y2), new PointF(x - dx * idx, y2) };
            else
                return new[] { new PointF(x + dx * idx, y), new PointF(x + dx * (idx + 1), y), new PointF(x + dx * (idx + 1), y2), new PointF(x + dx * idx, y2) };

        }

    }
}
