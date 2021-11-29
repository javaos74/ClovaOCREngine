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
        internal static UiPath.OCR.Contracts.OCRRotation GetOCRRotation(Single rot)
        {
#if DEBUG
            System.Console.WriteLine(" roation : " + rot);
#endif
            if (rot >= 88 && rot <= 92)
                return OCRRotation.Rotated90;
            else if (rot >= 178 && rot <= 182)
                return OCRRotation.Rotated180;
            else if (rot >= 268 && rot <= 272)
                return OCRRotation.Rotated270;
            else if (rot >= 358 || rot <= 2)
                return OCRRotation.None;
            else
                return OCRRotation.Other;
        }

        internal static async Task<OCRResult> FromClient(string file_path, Dictionary<string, object> options)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Clear();
            //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

            var filebytes = File.ReadAllBytes(file_path);
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
                List<JObject> words = new List<JObject>();

                foreach( var l in lines)
                {
                    var ws = (JArray)l["words"];
                    foreach (var w in ws)
                        words.Add((JObject)w);
                    sb.Append((string)l["text"]);
                }

                ocrResult.Words = words.Select(p => new Word
                {
                    Text = (string)p["text"],
                    PolygonPoints = GenBoundingBox((JArray)p["boundingBox"]),
                    Confidence = Convert.ToInt32((float)p["confidence"] * 100),
                    Characters = GetCharacters( (string)p["text"], Convert.ToInt32((float)p["confidence"] * 100), GenBoundingBox((JArray)p["boundingBox"]))
                }).ToArray();

                ocrResult.Text = sb.ToString();
                ocrResult.SkewAngle = (int)respJson["analyzeResult"]["readResults"][0]["angle"];
                ocrResult.Confidence = 0;
            }
            return ocrResult;
        }

        internal static Character[] GetCharacters( string text, int confidence, PointF[] points)
        {
#if DEBUG
            Console.WriteLine(text + "/" + confidence.ToString());
#endif
            var listChars = new List<Character>();
            try
            {
                if( text.Length == 1)
                {
                    listChars.Add(new Character
                    {
                        Char = text.ElementAt(0),
                        Confidence = confidence,
                        PolygonPoints = points
                    });
                }
                else
                {
                    var idx = 0;
                    foreach( var ch in text)
                    {
                        listChars.Add(new Character
                        {
                            Char = ch,
                            Confidence = confidence,
                            PolygonPoints = ReduceBoundingBox(points, idx, text.Length)
                        });
                        idx++;
                    }
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine(e.StackTrace);
#endif
            }

            return listChars.ToArray();
        }

        internal static Character[] GetCharacters(JArray words)
        {
            var listChars = new List<Character>();
            try
            {
                foreach (var word in words)
                {
                    var text = (string)word["text"];
#if DEBUG
                    //Console.WriteLine(" current words: " + text);
#endif
                    if (text.Length == 1)
                    {
                        listChars.Add(new Character
                        {
                            Char = text.ElementAt(0),
                            Confidence = Convert.ToInt32((float)word["confidence"] * 100),
                            PolygonPoints = GenBoundingBox((JArray)word["boundingBox"])
                        });
                    }
                    else
                    {
                        var idx = 0;
                        PointF[] points = GenBoundingBox((JArray)word["boundingBox"]);
                        foreach (var ch in text)
                        {
                            listChars.Add(new Character
                            {
                                Char = ch,
                                Confidence = Convert.ToInt32((float)word["confidence"] * 100),
                                PolygonPoints = ReduceBoundingBox(points, idx, text.Length)
                            });
                            idx++;
                        }
                    }
                }
            } catch (Exception e)
            {
#if DEBUG
                Console.WriteLine(e.StackTrace);
#endif
            }

            return listChars.ToArray();
        }
        internal static int GetAverageConfidence(JArray words)
        {
            return Convert.ToInt32(words.Average(p => (float)p["confidence"]) * 100);
        }
        internal static PointF[] GenBoundingBox(JArray points)
        {
            return new[] { new PointF(Convert.ToSingle(points.ElementAt(0).ToString()), Convert.ToSingle(points.ElementAt(1).ToString())),
                          new PointF(Convert.ToSingle(points.ElementAt(2).ToString()), Convert.ToSingle(points.ElementAt(3).ToString())),
                          new PointF(Convert.ToSingle(points.ElementAt(4).ToString()), Convert.ToSingle(points.ElementAt(5).ToString())),
                          new PointF(Convert.ToSingle(points.ElementAt(6).ToString()), Convert.ToSingle(points.ElementAt(7).ToString()))
                };
        }

        internal static PointF[] ReduceBoundingBox(PointF [] points, int idx, int total)
        {
            var x = points[0].X;
            var y = points[0].Y;
            var w = Math.Abs(points[1].X - x);
            var y2 = points[3].Y;

            float dx = w / total;
            float dy = Math.Abs(y2 - y) / total;

            return new[] {
               new PointF(x + dx * idx, y), new PointF(x + dx * (idx + 1), y), new PointF(x + dx * (idx + 1), y2), new PointF(x + dx * idx, y2) 
            };
        }
    }
}
