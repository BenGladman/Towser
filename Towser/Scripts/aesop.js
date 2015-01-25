var aesopInit = function () {

    var vt100 = new VT100(132, 24, 'terminal');

    var refreshTimeoutId = null;

    var refreshFunc = function () {
        refreshTimeoutId = null;
        vt100.refresh();
    };

    var refresh = function () {
        if (refreshTimeoutId == null) {
            refreshTimeoutId = window.setTimeout(refreshFunc, 10);
        }
    };

    var move = function (movemode, moverow, movecol) {
        switch (movemode) {
            case 1:
                // row and col
                vt100.move(moverow, movecol);
                break;
            case 2:
                // row
                vt100.move(moverow, vt100.col_);
                break;
            case 3:
                // relative row
                vt100.move(moverow + vt100.row_, vt100.col_);
                break;
            case 4:
                // col
                vt100.move(vt100.row_, movecol);
                break;
            case 5:
                // relative col
                vt100.move(vt100.row_, movecol + vt100.col_);
                break;
        }
    };

    var clear = function (clearmode) {
        switch (clearmode) {
            case 1: vt100.clear(); break;
            case 2: vt100.clrtoeol(); break;
            case 3: vt100.clrtobot(); break;
        }
    };

    var setSgr = function (sgr) {
        switch (sgr) {
            case 0:
                vt100.standend();
                vt100.fgset(vt100.bkgd_.fg);
                vt100.bgset(vt100.bkgd_.bg);
                break;
            case 1:
                vt100.attroff(VT100.A_DIM);
                vt100.attron(VT100.A_BOLD);
                break;
            case 2:
                vt100.attroff(VT100.A_BOLD)
                vt100.attron(VT100.A_DIM);
                break;
            case 4:
                vt100.attron(VT100.A_UNDERLINE);
                break;
            case 5:
                vt100.attron(VT100.A_BLINK);
                break;
            case 7:
                vt100.attron(VT100.A_REVERSE);
                break;
            case 21:
                vt100.attroff(VT100.A_BOLD);
                break;
            case 22:
                vt100.attroff(VT100.A_BOLD);
                vt100.attroff(VT100.A_DIM);
                break;
            case 24:
                vt100.attroff(VT100.A_UNDERLINE);
                break;
            case 25:
                vt100.attroff(VT100.A_BLINK);
                break;
            case 27:
                vt100.attroff(VT100.A_REVERSE);
                break;
            case 30:
                vt100.fgset(VT100.COLOR_BLACK);
                break;
            case 31:
                vt100.fgset(VT100.COLOR_RED);
                break;
            case 32:
                vt100.fgset(VT100.COLOR_GREEN);
                break;
            case 33:
                vt100.fgset(VT100.COLOR_YELLOW);
                break;
            case 34:
                vt100.fgset(VT100.COLOR_BLUE);
                break;
            case 35:
                vt100.fgset(VT100.COLOR_MAGENTA);
                break;
            case 36:
                vt100.fgset(VT100.COLOR_CYAN);
                break;
            case 37:
                vt100.fgset(VT100.COLOR_WHITE);
                break;
            case 39:
                vt100.fgset(vt100.bkgd_.fg);
                break;
            case 40:
                vt100.bgset(VT100.COLOR_BLACK);
                break;
            case 41:
                vt100.bgset(VT100.COLOR_RED);
                break;
            case 42:
                vt100.bgset(VT100.COLOR_GREEN);
                break;
            case 43:
                vt100.bgset(VT100.COLOR_YELLOW);
                break;
            case 44:
                vt100.bgset(VT100.COLOR_BLUE);
                break;
            case 45:
                vt100.bgset(VT100.COLOR_MAGENTA);
                break;
            case 46:
                vt100.bgset(VT100.COLOR_CYAN);
                break;
            case 47:
                vt100.bgset(VT100.COLOR_WHITE);
                break;
            case 49:
                vt100.bgset(vt100.bkgd_.bg);
                break;
        }
    };

    var setSgrs = function (sgrs) {
        sgrs.forEach(setSgr);
    };

    var processFragment = function (fragment) {
        if (fragment.t) { vt100.addstr(fragment.t); }
        move(fragment.m, fragment.mr || 0, fragment.mc || 0);
        clear(fragment.c);
        if (fragment.sgr) { setSgrs(fragment.sgr); }
    };

    var hub = $.connection.aesopHub;

    hub.client.write = function (data) {
        data.forEach(processFragment);
        refresh();
    }

    $.connection.hub.start()
        .done(function(){ console.log('Now connected, connection ID=' + $.connection.hub.id); })
        .fail(function(){ console.log('Could not Connect!'); });

    var sendFunction = function (ch) {
        console.log("send " + ch);
        hub.server.keyPress(ch);
    };

    var onKeyHandler = keyboardInit(sendFunction);
    window.addEventListener("keypress", onKeyHandler, false);
    window.addEventListener("keydown", onKeyHandler, false);

    vt100.noecho();
    window.document.getElementById("buttons").style.display = "none";
}