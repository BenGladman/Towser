var bridgInit = function () {
    var term = new Terminal({
        cols: 132,
        rows: 24,
        useStyle: true,
        screenKeys: true,
        cursorBlink: false,
        colors: Terminal.xtermColors
    });

    //override term.js keyboard & mouse handling
    keyboardInit(term);
    mouseInit(term);

    term.open(document.body);

    var connection = $.connection('/bridg');

    // receive from signalR
    connection.received(function (data) {
        term.write(data);
    });

    connection.start().done(function () {
        console.log('Now connected');

        // receive from terminal
        term.on("data", function (data) {
            connection.send(data);
        });
    });

    window.document.getElementById("buttons").style.display = "none";
}