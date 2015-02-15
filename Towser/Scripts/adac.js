var adacInit = function () {
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

    term.open(document.body);

    var hub = $.connection.adacHub;

    hub.client.write = function (data) {
        term.write(data);
    }

    $.connection.hub.start()
        .done(function () {
            console.log('Now connected, connection ID=' + $.connection.hub.id);

            // receive from terminal
            term.on("data", function (data) {
                hub.server.keyPress(data);
            });
        })

        .fail(function () {
            console.log('Could not Connect!');
        });

    window.document.getElementById("buttons").style.display = "none";
}