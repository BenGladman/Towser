var towserPconInit = function () {
    var term = towserTermInit();

    var connection = $.connection('/towserPcon');

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

    term.onreset = function () { connection.stop(); }
}