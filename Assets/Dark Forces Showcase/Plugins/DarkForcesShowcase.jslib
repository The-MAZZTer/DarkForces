mergeInto(LibraryManager.library, {
	GetUploadFileContents: function(path, handle, buffer, bufferLength, callback) {
		window.GetUploadFileContents(UTF8ToString(path), HEAPU8.subarray(buffer, buffer + bufferLength), function(success) {
			dynCall_vii(callback, handle, success)
		});
	},
	DeleteDownloadFile: function(path) { window.DeleteDownloadFile(UTF8ToString(path)) },
	CreateDownloadFolder: function(path) { window.CreateDownloadFolder(UTF8ToString(path)) },
	SetDownloadFile: function(path, length) { window.SetDownloadFile(UTF8ToString(path), length) },
	ShowDownload: function(path) { window.ShowDownload(UTF8ToString(path)) },
	Download: function(path, handle, buffer, bufferLength, callback) {
		window.Download(UTF8ToString(path), HEAPU8.subarray(buffer, buffer + bufferLength), function() {
			dynCall_vi(callback, handle);
		});
	}
});
