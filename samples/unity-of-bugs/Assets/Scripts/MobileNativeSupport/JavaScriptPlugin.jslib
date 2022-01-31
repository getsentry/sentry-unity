mergeInto(LibraryManager.library, {

  ThrowJavaScript: function () {
    var something = undefined;
    console.log("JavaScript error incoming...");
    something.do();
  },

});
