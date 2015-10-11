var towserPconInit = function () {
    var term = towserTermInit();

    var connection = $.connection('/towserPcon');

    // receive from signalR
    connection.received(function (data) {
        term.write(data);
    });

    connection.start()
        .done(function () {
            console.log('Now connected, connection id=' + connection.id);
        })
        .fail(function () {
            console.log('Could not connect!');
        });

    // receive from terminal
    term.oninput = function (data) { connection.send(data); }

    // stop terminal
    term.onstop = function () { connection.stop(); }
}