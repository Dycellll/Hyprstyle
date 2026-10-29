import QtQuick
import QtQuick.Effects
import Quickshell
import Quickshell.Io
import Quickshell.Wayland

ShellRoot {
    id: root

    readonly property string dir: Quickshell.env("HYPRSTYLE_TRANSITION_DIR")

    property bool loading: false
    property bool shown: false
    property bool armed: false
    property int loaded: 0
    property real cursorX: 0
    property real cursorY: 0
    property string kind: "circle"
    property int duration: 600
    property real progress: 0
    property real fraction: 0
    property real dim: 0
    property bool barOn: false
    property string last: ""
    property double changedAt: 0
    property double goAt: 0
    property var peer: null

    readonly property var reveals: ({
        "circle": (p, w, h, cx, cy, reach) => ({ x: cx - reach * p, y: cy - reach * p, width: reach * p * 2, height: reach * p * 2, radius: reach * p }),
        "circle-centered": (p, w, h) => {
            const r = (Math.hypot(w, h) / 2 + 2) * p
            return { x: w / 2 - r, y: h / 2 - r, width: r * 2, height: r * 2, radius: r }
        },
        "horizontal": (p, w, h) => ({ width: w * p, height: h }),
        "vertical": (p, w, h) => ({ width: w, height: h * p }),
        "diagonal": (p, w, h) => {
            const d = Math.hypot(w, h)
            const thickness = d * 2
            const length = d * 3
            const cx = p * w - h / d * thickness / 2
            const cy = p * h - w / d * thickness / 2
            return { x: cx - length / 2, y: cy - thickness / 2, width: length, height: thickness, rotation: Math.atan2(-h, w) * 180 / Math.PI }
        }
    })

    function shapeFor(p, w, h, cx, cy, reach) {
        const make = reveals[kind] || reveals.circle
        const base = { x: 0, y: 0, width: 0, height: 0, radius: 0, rotation: 0 }
        return Object.assign(base, make(p, w, h, cx, cy, reach))
    }

    readonly property real reach: {
        let far = 0
        for (const s of Quickshell.screens) {
            for (const x of [s.x, s.x + s.width]) {
                for (const y of [s.y, s.y + s.height])
                    far = Math.max(far, Math.hypot(x - cursorX, y - cursorY))
            }
        }
        return far + 2
    }

    function say(text) {
        if (peer === null)
            return
        peer.write(text + "\n")
        peer.flush()
    }

    function handle(line) {
        const parts = line.trim().split(" ")

        if (parts[0] === "show") {
            kind = parts[1]
            duration = Number(parts[2])
            loading = true
        } else if (parts[0] === "progress") {
            fraction = Number(parts[1])
        } else if (parts[0] === "go") {
            armed = true
            goAt = Date.now()
            changedAt = goAt
            probe.running = true
        }
    }

    function checked(text) {
        const now = Date.now()

        if (text !== last) {
            last = text
            changedAt = now
        }

        if (now - changedAt >= 170 || now - goAt >= 2000)
            reveal()
        else
            pollTimer.start()
    }

    function reveal() {
        barOn = false
        cursorProbe.running = true
    }

    function start(text) {
        try {
            const cursor = JSON.parse(text)
            cursorX = cursor.x
            cursorY = cursor.y
        } catch (e) {
        }

        lifetime.interval = duration + 1500
        lifetime.restart()
        animation.start()
    }

    Timer {
        id: lifetime
        interval: 8000
        running: true
        onTriggered: Qt.quit()
    }

    Timer {
        id: readyTimer
        interval: 40
        onTriggered: {
            root.say("ready")
            barDelay.start()
            dimIn.start()
        }
    }

    Timer {
        id: barDelay
        interval: 250
        onTriggered: root.barOn = true
    }

    NumberAnimation {
        id: dimIn
        target: root
        property: "dim"
        to: 1
        duration: 1200
        easing.type: Easing.OutCubic
    }

    Behavior on fraction {
        NumberAnimation { duration: 300; easing.type: Easing.OutCubic }
    }

    Timer {
        id: pollTimer
        interval: 20
        onTriggered: probe.running = true
    }

    Process {
        id: probe
        command: ["sh", "-c", "hyprctl clients -j; hyprctl layers -j"]
        stdout: StdioCollector {
            onStreamFinished: root.checked(text)
        }
    }

    Process {
        id: cursorProbe
        command: ["hyprctl", "cursorpos", "-j"]
        stdout: StdioCollector {
            onStreamFinished: root.start(text)
        }
    }

    NumberAnimation {
        id: animation
        target: root
        property: "progress"
        from: 0
        to: 1
        duration: root.duration
        easing.type: Easing.InOutCubic
        onFinished: Qt.quit()
    }

    SocketServer {
        active: true
        path: root.dir + "/sock"

        handler: Socket {
            parser: SplitParser {
                onRead: line => root.handle(line)
            }

            Component.onCompleted: root.peer = this

            onConnectedChanged: {
                if (!connected && !root.armed)
                    Qt.quit()
            }
        }
    }

    Variants {
        model: Quickshell.screens

        PanelWindow {
            id: window
            required property var modelData
            screen: modelData
            visible: root.shown
            color: "transparent"
            mask: Region {}
            exclusionMode: ExclusionMode.Ignore
            WlrLayershell.namespace: "hyprstyle-transition"
            WlrLayershell.layer: WlrLayer.Overlay
            WlrLayershell.keyboardFocus: WlrKeyboardFocus.None

            anchors {
                top: true
                bottom: true
                left: true
                right: true
            }

            readonly property var cut: root.shapeFor(
                root.progress, width, height, root.cursorX - modelData.x, root.cursorY - modelData.y, root.reach)

            Image {
                id: shot
                anchors.fill: parent
                visible: false
                cache: false
                source: root.loading ? "file://" + root.dir + "/" + window.modelData.name + ".ppm" : ""

                onStatusChanged: {
                    if (status === Image.Error) {
                        root.say("fail")
                        Qt.quit()
                    } else if (status === Image.Ready) {
                        root.loaded++
                        if (root.loaded === Quickshell.screens.length) {
                            root.shown = true
                            readyTimer.start()
                        }
                    }
                }
            }

            Item {
                id: hole
                anchors.fill: parent
                visible: false
                layer.enabled: true

                Rectangle {
                    color: "white"
                    x: window.cut.x
                    y: window.cut.y
                    width: window.cut.width
                    height: window.cut.height
                    radius: window.cut.radius
                    rotation: window.cut.rotation
                }
            }

            MultiEffect {
                anchors.fill: parent
                source: shot
                maskEnabled: true
                maskSource: hole
                maskInverted: true
                brightness: -0.1 * root.dim * (1 - root.progress)
            }

            Rectangle {
                x: (parent.width - width) / 2
                y: parent.height - 80
                width: 180
                height: 4
                radius: 2
                color: "#40ffffff"
                opacity: root.barOn ? 1 : 0

                Behavior on opacity { NumberAnimation { duration: 180 } }

                Rectangle {
                    width: Math.max(0.06, root.fraction) * parent.width
                    height: parent.height
                    radius: 2
                    color: "#7aa2f7"

                    SequentialAnimation on opacity {
                        loops: Animation.Infinite
                        running: root.barOn
                        NumberAnimation { to: 0.65; duration: 700; easing.type: Easing.InOutSine }
                        NumberAnimation { to: 1; duration: 700; easing.type: Easing.InOutSine }
                    }
                }
            }
        }
    }
}
