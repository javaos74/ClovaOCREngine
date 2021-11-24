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
using System.Net.Http;
using System.Net.Http.Headers;

namespace ClovaOCRActivities.Basic.OCR
{
    internal static class READOCRResultHelper
    {
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

        internal static async Task<OCRResult> FromClient(string file_path, Dictionary<string, object> options)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Clear();
            //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

            var filebytes = File.ReadAllBytes( file_path);
            var endpoint = options["endpoint"].ToString().EndsWith("/") ? options["endpoint"].ToString() + "vision/v3.2/read/syncAnalyze" : options["endpoint"].ToString() + "/vision/v3.2/read/syncAnalyze";
            if (!string.IsNullOrEmpty(options["lang"].ToString()))
                endpoint += "?language=" + options["lang"].ToString();
            HttpResponseMessage resp;
            using (var content = new ByteArrayContent(filebytes))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                resp = await client.PostAsync(endpoint, content);
            }
            OCRResult ocrResult = new OCRResult();

            if (resp.StatusCode == HttpStatusCode.OK)
            {
                var body = await resp.Content.ReadAsStringAsync();
#if DEBUG
                System.Console.WriteLine(resp.StatusCode + " == > " + (body.Length > 100 ? body.Substring(0, 100) : body));
                System.IO.File.WriteAllText(@"C:\Temp\READ_resp.json", body);
#endif
                StringBuilder sb = new StringBuilder();
                JObject respJson = JObject.Parse(body);
                JArray lines = (JArray)respJson["analyzeResult"]["readResults"][0]["lines"];
                ocrResult.Words = lines.Select(p => new Word
                {
                    Text = (string)p["text"],
                    Characters = ((JArray)p["words"]).Select(ch => new Character
                    {
                        Char = (Char)ch["text"].ToString().ElementAt(0),
                        Confidence = (int) ((float)ch["confidence"] * 100),
                        PolygonPoints = genboxes( (JArray) p["boundingBox"])
                    }).ToArray()
                }).ToArray();
                foreach (var l in lines)
                {
                    sb.Append((string)l["text"]);
                    sb.Append(((Boolean)l["text"]) ? Environment.NewLine : " ");
                }
                ocrResult.Text = sb.ToString();
                ocrResult.SkewAngle = (int)respJson["angle"];
                ocrResult.Confidence = 0;
            }
            return ocrResult;
        }

        internal static PointF[] genboxes(JArray points)
        {
            return new[] { new PointF(Convert.ToSingle(points.ElementAt(0).ToString()), Convert.ToSingle(points.ElementAt(1).ToString())),
                          new PointF(Convert.ToSingle(points.ElementAt(2).ToString()), Convert.ToSingle(points.ElementAt(3).ToString())),
                          new PointF(Convert.ToSingle(points.ElementAt(4).ToString()), Convert.ToSingle(points.ElementAt(5).ToString())),
                          new PointF(Convert.ToSingle(points.ElementAt(6).ToString()), Convert.ToSingle(points.ElementAt(7).ToString()))
                };
        }
    }
}
