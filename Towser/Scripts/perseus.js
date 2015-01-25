var perseusInit = function () {
    var term = new Terminal({
        cols: 132,
        rows: 24,
        useStyle: true,
        screenKeys: true,
        cursorBlink: false
    });

    var connection = $.connection('/perseus');

    //override term.js keyboard handling
    var emitFunction = function (ch) { term.emit("data", ch); };
    var onKeyHandler = keyboardInit(emitFunction);
    term.keyDown = onKeyHandler;
    term.keyPress = onKeyHandler;

    // receive from terminal
    term.on("data", function (data) {
        connection.send(data);
    });

    // receive from signalR
    connection.received(function (data) {
        term.write(data);
    });

    term.open(document.body);

    connection.start().done(function () {
        $("#buttons").hide();
    });
}