mergeInto(LibraryManager.library, {
  DownloadFile: function (fileNamePtr, dataPtr, dataLength) {
    var fileName = UTF8ToString(fileNamePtr);
    var data = new Uint8Array(Module.HEAPU8.buffer, dataPtr, dataLength);

    var blob = new Blob([data], { type: "application/octet-stream" });
    var url = URL.createObjectURL(blob);

    var a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);

    URL.revokeObjectURL(url);
  }
});