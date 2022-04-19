mergeInto(LibraryManager.library, {

  throwJavaScript: function () {
    var something = undefined;
    // Note: if we trigger the JS error by calling `something.do();` directly here, Unity get's stuck:
    // An abnormal situation has occurred: the PlayerLoop internal function has been called recursively. Please contact Customer Support with a sample project so that we can reproduce the problem and troubleshoot it.
    console.log("Scheduling a JavaScript error");
    setTimeout(function(){
        console.log("JavaScript error incoming...");
        something.do();
    }, 0);
  },

});
