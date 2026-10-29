import QtQuick
import QtQuick.Layouts

RowLayout {
    id: row
    property string label: ""
    property bool on: false
    property string value: ""
    property string hint: ""
    signal switched(bool value)
    signal edited(string text)

    spacing: 10

    Toggle {
        checked: row.on
        onToggled: value => row.switched(value)
    }

    Text {
        Layout.preferredWidth: 124
        text: row.label
        color: row.on ? "#c4c4cc" : "#7a7a84"
        font.pixelSize: 13
        elide: Text.ElideRight

        Behavior on color { ColorAnimation { duration: 140 } }
    }

    Input {
        id: field
        Layout.fillWidth: true
        hint: row.hint
        enabled: row.on
        opacity: row.on ? 1 : 0.4
        onEdited: row.edited(text)
        Component.onCompleted: text = row.value

        Behavior on opacity { NumberAnimation { duration: 140 } }
    }
}
