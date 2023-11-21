using MZZT.IO.FileProviders;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using UnityEngine;

namespace MZZT.IO.FileSystemProviders {
	[DataContract]
	public class WebFileUpload {
		[DataMember(Name = "name")]
		public string Name { get; set; }
		[DataMember(Name = "size")]
		public long Size { get; set; }
	}

	public class WebFileSystemProviderBrowserCallback : MonoBehaviour {
		public static WebFileSystemProviderBrowserCallback Instance { get; private set; }

		private void OnEnable() {
			if (Instance == null) {
				Instance = this;
			}
		}

		private void OnDisable() {
			if (Instance == this) {
				Instance = null;
			}
		}

		public bool BrowserFilesUploaded { get; private set; }

		public void OnBrowserUploadedFiles(string json) {
			DataContractJsonSerializer serializer = new(typeof(WebFileUpload[]), new DataContractJsonSerializerSettings() {
				UseSimpleDictionaryFormat = true
			});
			WebFileUpload[] files;
			using (MemoryStream mem = new(Encoding.UTF8.GetBytes(json))) {
				files = (WebFileUpload[])serializer.ReadObject(mem);
			}
						
			((WebFileSystemProvider)FileManager.Instance.Provider).OnBrowserUploadedFiles(files);

			this.BrowserFilesUploaded = true;
		}

		public void OnBrowserDeleteFile(string path) =>
			WebFileSystemProvider.Instance.OnBrowserDeleteFile(path.Replace('/', Path.DirectorySeparatorChar));

		public async void OnBrowserDownloadFile(string path) =>
			await WebFileSystemProvider.Instance.OnBrowserDownloadFileAsync(path.Replace('/', Path.DirectorySeparatorChar));
	}
}
