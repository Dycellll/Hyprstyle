import QtQuick

Rectangle {
    id: root
    property var options: []
    property int current: 0
    signal picked(int index)

    readonly property real cell: 66

    implicitWidth: cell * options.length + 4
    implicitHeight: 30
    radius: 4
    color: "#121216"
    border.width: 1
    border.color: "#2e2e36"

    Rectangle {
        x: 2 + root.current * root.cell
        y: 2
        width: root.cell
        height: root.height - 4
        radius: 3
        color: "#7aa2f7"

        Behavior on x { NumberAnimation { duration: 170; easing.type: Easing.OutCubic } }
    }

    Row {
        x: 2
        y: 2

        Repeater {
            model: root.options

            delegate: Item {
                id: option
                required property string modelData
                required property int index
                width: root.cell
                height: root.height - 4

                Text {
                    anchors.centerIn: parent
                    text: option.modelData
                    font.pixelSize: 13
                    color: root.current === option.index ? "#14141a" : optionHit.containsMouse ? "#e6e6ee" : "#a8a8b2"

                    Behavior on color { ColorAnimation { duration: 140 } }
                }

                MouseArea {
                    id: optionHit
                    anchors.fill: parent
                    hoverEnabled: true
                    cursorShape: Qt.PointingHandCursor
                    onClicked: root.picked(option.index)
                }
            }
        }
    }
}
