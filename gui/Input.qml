import QtQuick

Rectangle {
    id: box
    property alias text: edit.text
    property string hint: ""
    property bool error: false
    property int fontSize: 13
    signal accepted()
    signal edited()

    function focusInput() {
        edit.forceActiveFocus()
    }

    implicitHeight: 30
    radius: 4
    color: "#1b1b20"
    border.width: 1
    border.color: error ? "#e06c75" : edit.activeFocus ? "#7aa2f7" : "#2e2e36"

    Behavior on border.color { ColorAnimation { duration: 160 } }

    TextInput {
        id: edit
        anchors.fill: parent
        anchors.leftMargin: 8
        anchors.rightMargin: 8
        verticalAlignment: TextInput.AlignVCenter
        color: "#d4d4dc"
        selectionColor: "#7aa2f7"
        selectedTextColor: "#14141a"
        font.pixelSize: box.fontSize
        clip: true
        onAccepted: box.accepted()
        onTextEdited: box.edited()
    }

    Text {
        anchors.fill: edit
        verticalAlignment: Text.AlignVCenter
        text: box.hint
        color: "#62626c"
        font.pixelSize: box.fontSize
        elide: Text.ElideRight
        opacity: edit.text === "" ? 1 : 0

        Behavior on opacity { NumberAnimation { duration: 120 } }
    }
}
