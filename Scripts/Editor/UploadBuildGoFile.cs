// using System;
// using System.IO;
// using System.Net.Http;
// using System.Text;
// using System.Threading.Tasks;
// using Newtonsoft.Json.Linq;
// using UnityEditor;
//
// public class UploadToGofile
// {
//     [MenuItem("Build/UploadGoFile")]
//     static async void Upload()
//     {
//         var folderId = "";
//         var filePath = "";
//         var apiKey   = "";
//
//
//         using (HttpClient client = new HttpClient())
//         {
//             try
//             {
//                 client.Timeout = TimeSpan.FromMinutes(60); 
//                 CommonServices.LogMessage($"Start upload {filePath} to Gofile");
//                 var form       = new MultipartFormDataContent();
//                 var fileStream = File.OpenRead(filePath);
//                 var fileName   = Path.GetFileName(filePath);
//                 form.Add(new StreamContent(fileStream), "file", fileName);
//
//                 if (!string.IsNullOrEmpty(apiKey))
//                 {
//                     form.Add(new StringContent(apiKey), "token");
//                 }
//
//                 if (!string.IsNullOrEmpty(folderId))
//                 {
//                     form.Add(new StringContent(folderId), "folderId");
//                 }
//
//                 var response       = await client.PostAsync("https://upload.gofile.io/uploadfile", form);
//                 var responseString = await response.Content.ReadAsStringAsync();
//
//                 CommonServices.LogMessage("Server Response:\n" + responseString);
//
//                 var jsonResponse = JObject.Parse(responseString);
//
//                 if (jsonResponse["status"]?.ToString() == "ok")
//                 {
//                     var downloadPage = jsonResponse["data"]?["downloadPage"]?.ToString();
//                     CommonServices.LogMessage("✅ File đã được upload thành công!");
//                     CommonServices.LogMessage("🔗 Link tải: " + downloadPage);
//                 }
//                 else
//                 {
//                     CommonServices.LogMessage("❌ Upload thất bại!");
//                     CommonServices.LogMessage(responseString);
//                 }
//             }
//             catch (Exception ex)
//             {
//                 CommonServices.LogMessage("Lỗi: " + ex.Message);
//             }
//         }
//     }
//
//     public static async Task<string> SearchFolder(string token, string folderName)
//     {
//         using (HttpClient client = new HttpClient())
//         {
//             var jsonBody = new JObject
//             {
//                 ["query"] = folderName,
//                 ["token"] = token,
//                 ["types"] = new JArray("folder")
//             };
//
//             var content = new StringContent(jsonBody.ToString(), Encoding.UTF8, "application/json");
//
//             var response       = await client.PostAsync("https://api.gofile.io/contents/search", content);
//             var responseString = await response.Content.ReadAsStringAsync();
//
//             var json = JObject.Parse(responseString);
//
//             if (json["status"]?.ToString() == "ok")
//             {
//                 var results = json["data"]?["results"];
//
//                 if (results != null && results.HasValues)
//                 {
//                     foreach (var item in results)
//                     {
//                         string name = item["name"]?.ToString();
//                         string id   = item["id"]?.ToString();
//                         CommonServices.LogMessage($"📁 Tìm thấy folder: {name} | ID: {id}");
//
//                         return id; // Trả về folderId đầu tiên tìm thấy
//                     }
//                 }
//                 else
//                 {
//                     CommonServices.LogMessage("🔍 Không tìm thấy thư mục nào phù hợp.");
//                 }
//             }
//             else
//             {
//                 CommonServices.LogMessage("❌ Lỗi khi gọi API search: " + responseString);
//             }
//
//             return null;
//         }
//     }
// }