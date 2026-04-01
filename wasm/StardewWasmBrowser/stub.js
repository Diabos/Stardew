var MyLibrary = {
    $xtringToUTF8: function(str, outPtr, maxBytesToWrite) {
        return stringToUTF8Array(str, HEAPU8, outPtr, maxBytesToWrite);
    },
    $XTF8ToString: function(ptr, maxBytesToRead) {
        return UTF8ToString(ptr, maxBytesToRead);
    }
};
mergeInto(LibraryManager.library, MyLibrary);
