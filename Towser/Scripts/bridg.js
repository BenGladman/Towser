var bridgInit = function () {
    var term = new Terminal({
        cols: 132,
        rows: 24,
        useStyle: true,
        screenKeys: true,
        cursorBlink: false
    });

    //override term.js keyboard handling
    var emitFunction = function (ch) { term.emit("data", ch); };
    var onKeyHandler = keyboardInit(emitFunction);
    term.keyDown = onKeyHandler;
    term.keyPress = onKeyHandler;

    var connection = $.connection('/bridg');

    // receive from signalR
    connection.received(function (data) {
        term.write(data);
    });

    term.open(document.body);

    connection.start().done(function () {
        console.log('Now connected');

        // receive from terminal
        term.on("data", function (data) {
            connection.send(data);
        });

        $("#buttons").hide();
    });
}