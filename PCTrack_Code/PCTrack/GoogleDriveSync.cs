// This file is used to manage uploads, downloads and authentication to google drive
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using DotNetOpenAuth.OAuth2;
using Google.Apis.Authentication;
using Google.Apis.Authentication.OAuth2;
using Google.Apis.Authentication.OAuth2.DotNetOpenAuth;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Json;
using Google.Apis.Util;
using ActivityMonitor;
using System.Diagnostics;
using System.Windows;
using System.Threading;

namespace wpf
{
    /// <summary>
    /// This class contains methods to upload download and authenticate to google drive
    /// </summary>
    public class GoogleDriveSync
    {
        public static bool verificationEntered = false;
        public static string verificationString = "";
        public static Thread uploadThread = null;
        public static Thread downloadThread = null;
        private static volatile bool authProgress = false;

        [Serializable]
        public class SelfAuthorizationState : IAuthorizationState
        {
            public string AccessToken { get; set; }
            public DateTime? AccessTokenExpirationUtc { get; set; }
            public DateTime? AccessTokenIssueDateUtc { get; set; }
            public Uri Callback { get; set; }
            public string RefreshToken { get; set; }
            public HashSet<string> Scope { get { return new HashSet<string>(); } }

            public void Delete()
            {
                return;
            }
            public void SaveChanges()
            {
                return;
            }
        }

        /// <summary>
        /// create authenticator object gor google drive service
        /// </summary>
        /// <returns></returns>
        private static IAuthenticator createAuthenticator()
        {
            var provider = new NativeApplicationClient(GoogleAuthenticationServer.Description);
            provider.ClientIdentifier = "349699183942-1fo1k1b6c464e95cqrjfkn9sgeekklog.apps.googleusercontent.com";
            provider.ClientSecret = "8jJRhXy3oeT5uC8ji2qKxct5";
            return new OAuth2Authenticator<NativeApplicationClient>(provider, GetAuthorization);
        }

        /// <summary>
        /// gets google drive service object
        /// </summary>
        /// <returns></returns>
        private static DriveService getDriveService()
        {
            try
            {
                DriveService service = new DriveService(createAuthenticator());
                return service;
            }
            catch (Exception)
            {
                return null;
            }
            
        }

        /// <summary>
        /// gets authorization from google drive
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static IAuthorizationState GetAuthorization(NativeApplicationClient arg)
        {
            if (XmlDataLayer.GetConfigEntry("authentication") == "done"
                && System.IO.File.Exists(XmlDataLayer.GetUserApplicationDirectory() + "\\credentials"))
            {
                TaskCompletionSource<SelfAuthorizationState> tcs = new TaskCompletionSource<SelfAuthorizationState>();


                string filePath = XmlDataLayer.GetUserApplicationDirectory() + "\\credentials";

                var obj = System.IO.File.ReadAllText(filePath);
                tcs.SetResult(NewtonsoftJsonSerializer.Instance.Deserialize<SelfAuthorizationState>(obj));

                SelfAuthorizationState k = tcs.Task.Result;

                return k;
            }
            else
            {
                // Get the auth URL:
                IAuthorizationState state = new AuthorizationState(new[] { DriveService.Scopes.Drive.GetStringValue() });
                state.Callback = new Uri(NativeApplicationClient.OutOfBandCallbackUrl);
                Uri authUri = arg.RequestUserAuthorization(state);

                // Request authorization from the user (by opening a browser window):
                
                Process.Start(authUri.ToString());

                Thread response = new Thread(GetResponse);
                response.SetApartmentState(ApartmentState.STA);
                response.IsBackground = true;
                response.Start();
                while (verificationEntered != true)
                {
                    Thread.Sleep(200);
                }
                string authCode = verificationString;

                // Retrieve the access token by using the authorization code:
                IAuthorizationState s = arg.ProcessUserAuthorization(authCode, state);
                var serialized = NewtonsoftJsonSerializer.Instance.Serialize(s);
                System.IO.File.WriteAllText(XmlDataLayer.GetUserApplicationDirectory()+"\\credentials", serialized);
                return s;
            }
        }

        /// <summary>
        /// gets verification code from user
        /// </summary>
        /// <param name="obj"></param>
        private static void GetResponse(object obj)
        {
            Popup pop = new Popup();
            pop.Show();
            pop.Closed += pop_Closed;
            System.Windows.Threading.Dispatcher.Run();
        }

        /// <summary>
        /// called when verification window is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void pop_Closed(object sender, EventArgs e)
        {
            Popup s = sender as Popup;
            verificationString = s.verificationCode.Text;
            verificationEntered = true;
        }

        /// <summary>
        /// makes directory on google drive if it does not exist
        /// </summary>
        /// <param name="service"></param>
        /// <param name="folder"></param>
        /// <param name="parentId"></param>
        /// <returns></returns>
        private static File makeDirectoryIfNotExists(DriveService service, string folder, string parentId)
        {
            ChildrenResource.ListRequest request = service.Children.List(parentId);
            bool folderExists = false;
            File file = null;
            do
            {
                try
                {
                    ChildList children = request.Fetch();

                    foreach (ChildReference child in children.Items)
                    {
                        //Console.WriteLine("File Id: " + child.Id);
                        file = service.Files.Get(child.Id).Fetch();
                        string title = file.Title;
                        Console.WriteLine(title);
                        if (title == folder)
                        {
                            folderExists = true;
                            break;
                        }
                        //Console.WriteLine(title);
                        //File file = service.files().get(child.Id).execute();
                    }
                    request.PageToken = children.NextPageToken;
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    request.PageToken = null;
                }
            } while (!String.IsNullOrEmpty(request.PageToken));
            if (folderExists)
            {
                return file;
            }

            File body = new File();
            body.Title = folder;
            body.Description = "PCTrack_id " + XmlDataLayer.GetConfigEntry("id");
            body.MimeType = "application/vnd.google-apps.folder";

            // Set the parent folder.
            if (!String.IsNullOrEmpty(parentId))
            {
                body.Parents = new List<ParentReference>() { new ParentReference() { Id = parentId } };
            }


            try
            {
                file = service.Files.Insert(body).Fetch();
                return file;
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Main function which authenticates and creates threads for uploads and downloads if they are enabled
        /// </summary>
        /// <param name="createThreads"></param>
        public static void UploadDownloadNewFiles(bool createThreads = true)
        {
            if (authProgress)
            {
                return;
            }
            authProgress = true;
            DriveService service = getDriveService();
            if (service == null)
            {
                Thread.Sleep(5 * 60 * 1000);
                UploadDownloadNewFiles();
                return;
            }
            File file2 = null, tempFile = null;
            try
            {
                About ab = service.About.Get().Fetch();
                string rootId = ab.RootFolderId;

                file2 = makeDirectoryIfNotExists(service, "PCTrack", rootId);
                tempFile = makeDirectoryIfNotExists(service, "Temp", file2.Id);
            }
            catch (Exception)
            {
                XmlDataLayer.SetConfigEntry("authentication", "not_done");
                Thread.Sleep(5 * 60 * 1000);
                UploadDownloadNewFiles();
                return;
            }
            XmlDataLayer.SetConfigEntry("authentication", "done");

            if (createThreads)
            {
                try
                {
                    if (XmlDataLayer.GetConfigEntry("download_cloud") == "enabled")
                    {
                        downloadThread = new Thread(() => DownloadTempFiles(service, tempFile.Id));
                        downloadThread.Start();
                    }
                    if (XmlDataLayer.GetConfigEntry("upload_cloud") == "enabled")
                    {
                        uploadThread = new Thread(() => UploadTempFiles(service, tempFile.Id));
                        uploadThread.Start();
                    }
                }
                catch (Exception)
                {
                }
            }
            authProgress = false;
        }

        /// <summary>
        /// thread to upload temp files
        /// </summary>
        /// <param name="service"></param>
        /// <param name="folderId"></param>
        private static void UploadTempFiles(DriveService service, string folderId)
        {
            while (true)
            {
                string tempFolder = XmlDataLayer.GetUserApplicationDirectory() + "\\Temp";
                foreach (string f in System.IO.Directory.EnumerateFiles(tempFolder))
                {
                    try
                    {
                        string fileName = f.Split('\\')[f.Split('\\').Length - 1];
                        if (fileName == XmlDataLayer.GetCurrentTempFileName() || XmlDataLayer.GetUploadEntry(fileName) == true)
                        {
                            continue;
                        }
                        File body = new File();
                        body.Title = fileName;
                        body.Description = "PCTrack_id " + XmlDataLayer.GetConfigEntry("pc_id");
                        body.MimeType = "text/plain";
                        body.Parents = new List<ParentReference>() { new ParentReference() { Id = folderId } };
                        byte[] byteArray = System.IO.File.ReadAllBytes(f);
                        System.IO.MemoryStream stream = new System.IO.MemoryStream(byteArray);

                        FilesResource.InsertMediaUpload request = service.Files.Insert(body, stream, "text/plain");
                        request.Upload();

                        File file = request.ResponseBody;
                        if (file != null)
                        {
                            XmlDataLayer.SetUploadEntry(fileName);
                        }
                    }
                    catch (Exception)
                    {
                        UploadDownloadNewFiles(false);
                    }
                }
                Thread.Sleep(5 * 60 * 1000);
            }
        }

        /// <summary>
        /// thread to dowload temp files
        /// </summary>
        /// <param name="service"></param>
        /// <param name="parentId"></param>
        private static void DownloadTempFiles(DriveService service, string parentId)
        {
            while (true)
            {
                ChildrenResource.ListRequest request = service.Children.List(parentId);
                File file = null;
                do
                {
                    try
                    {
                        ChildList children = request.Fetch();

                        foreach (ChildReference child in children.Items)
                        {
                            //Console.WriteLine("File Id: " + child.Id);
                            file = service.Files.Get(child.Id).Fetch();
                            string title = file.Title.Replace(":", "_");
                            string id = file.Description.Split(' ')[1];
                            if (XmlDataLayer.CheckIfTempExists(title) == true || XmlDataLayer.GetDownloadEntry(title) == true || id == XmlDataLayer.GetConfigEntry("pc_id"))
                            {
                                continue;
                            }
                            try
                            {
                                System.IO.Stream tfile = DownloadFile(createAuthenticator(), file);
                                using (tfile)
                                {
                                    string filePath = XmlDataLayer.GetUserApplicationDirectory() + "\\Temp\\" + title;
                                    tfile.CopyTo(new System.IO.FileStream(filePath, System.IO.FileMode.Create, System.IO.FileAccess.Write));
                                    XmlDataLayer.SetDownloadEntry(title);
                                    XmlDataLayer x = new XmlDataLayer();
                                    x.CombineRecentResults();
                                }
                                Console.WriteLine(title);
                            }
                            catch (Exception)
                            {
                                UploadDownloadNewFiles(false);
                            }
                            //Console.WriteLine(title);
                            //File file = service.files().get(child.Id).execute();
                        }
                        request.PageToken = children.NextPageToken;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("An error occurred: " + e.Message);
                        request.PageToken = null;
                    }
                } while (!String.IsNullOrEmpty(request.PageToken));
                Thread.Sleep(5 * 60 * 1000);
            }
        }

        /// <summary>
        /// thread to download a file from google drive
        /// </summary>
        /// <param name="authenticator"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public static System.IO.Stream DownloadFile(IAuthenticator authenticator, File file)
        {
            if (!String.IsNullOrEmpty(file.DownloadUrl))
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                        new Uri(file.DownloadUrl));
                    authenticator.ApplyAuthenticationToRequest(request);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return response.GetResponseStream();
                    }
                    else
                    {
                        Console.WriteLine(
                            "An error occurred: " + response.StatusDescription);
                        return null;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    return null;
                }
            }
            else
            {
                // The file doesn't have any content stored on Drive.
                return null;
            }
        }
    }
}