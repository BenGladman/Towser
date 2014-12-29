var hubInit = function () {

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

    var setAttr = function (attr) {
        switch (attr) {
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

    var setAttrs = function (attrs) {
        attrs.forEach(setAttr);
    };

    var processFragment = function (fragment) {
        if (fragment.t) { vt100.addstr(fragment.t); }
        move(fragment.m, fragment.mr || 0, fragment.mc || 0);
        clear(fragment.c);
        if (fragment.a) { setAttrs(fragment.a); }
    };

    var hub = $.connection.towserHub;

    hub.client.write = function (data) {
        data.forEach(processFragment);
        refresh();
    }

    $.connection.hub.start()
        .done(function(){ console.log('Now connected, connection ID=' + $.connection.hub.id); })
        .fail(function(){ console.log('Could not Connect!'); });

    var keymap = {
        // backspace
        8: '\b',
        // tab
        9: '\t',
        // return
        13: '\r\n',
        // escape
        27: '\x1b',
        // page up
        33: '\x1b[5~',
        // page down
        34: '\x1b[6~',
        // end
        35: '\x1b[4~',
        // home
        36: '\x1b[1~',
        // down
        39: '\x1b[C',
        // left
        37: '\x1b[D',
        // up
        38: '\x1b[A',
        // right
        40: '\x1b[B',
        // insert
        45: '\x1b[2~',
        // delete
        46: '\x1b[3~',
        // f1
        112: '\x1bOP',
        // f2
        113: '\x1bOQ',
        // f3
        114: '\x1bOR',
        // f4
        115: '\x1bOS',
        // f5 (send f14 sequence)
        116: '\x1b[26~',
        // f6
        117: '\x1b[17~',
        // f7
        118: '\x1b[18~',
        // f8
        119: '\x1b[19~',
        // f9
        120: '\x1b[20~',
        // f10
        121: '\x1b[21~',

        // don't intercept f11 and f12 - let the browser use them
        // f11
        //122: '\x1b[22~',
        // f12
        //123: '\x1b[23~',
    };

    // send to signalR
    var onKeyHandler = function (event) {
        if (!vt100) { return true; }

        var ch;

        if (event.type === "keypress") {
            // printable characters
            var charcode = event.charCode;
            console.info("keypress " + charcode);

            if (charcode < 32) { return true; }
            if (charcode === 127) { return true; }
            if (charcode > 255) { return true; }
            ch = String.fromCharCode(charcode);

        } else if (event.type === "keydown") {
            var kc = event.keyCode;
            console.info("keydown " + kc);

            if (kc >= 65 && kc <= 90 && event.ctrlKey && !event.shiftKey) {
                // ctrl-key input
                ch = String.fromCharCode(kc - 64);

            } else {
                ch = keymap[kc];
                if (ch === undefined) { return true; }
            }
        }

        if (ch) {
            console.log("send " + ch);
            hub.server.keyPress(ch);
        }

        event.preventDefault();
        return false;
    }

    vt100.noecho();
    window.addEventListener("keypress", onKeyHandler, false);
    window.addEventListener("keydown", onKeyHandler, false);
    window.document.getElementById("buttons").style.display = "none";
}