mergeInto(LibraryManager.library, {
  HelloString: function (str) {
    window.alert(Pointer_stringify(str));
  },

  SendMessageToWebAsJSON: function (str) {
    var msgString = Pointer_stringify(str);
    //console.log("Sending from UNITY: " + msgString);
    if (typeof window.RMQ === "undefined") window.RMQ = [];
    window.RMQ.push(msgString);
  }
});
