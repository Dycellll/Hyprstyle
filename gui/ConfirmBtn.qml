import QtQuick

Btn {
    id: root
    property string idleLabel: ""
    property string askLabel: "Click again to confirm"
    property bool asking: false
    signal confirmed()

    label: asking ? askLabel : idleLabel
    danger: asking

    onClicked: {
        if (!asking) {
            asking = true
            reset.restart()
            return
        }

        asking = false
        reset.stop()
        confirmed()
    }

    Timer {
        id: reset
        interval: 3000
        onTriggered: root.asking = false
    }
}
