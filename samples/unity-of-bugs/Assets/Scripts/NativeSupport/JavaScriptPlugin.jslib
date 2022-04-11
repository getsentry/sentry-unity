mergeInto(LibraryManager.library, {

  throwJavaScript: function () {
    var something = undefined;
    console.log("JavaScript error incoming...");
    something.do();
  },

});
