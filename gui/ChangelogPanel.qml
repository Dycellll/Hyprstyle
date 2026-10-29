import QtQuick
import QtQuick.Layouts

Item {
    id: root
    property bool open: false
    property var entries: []
    signal closed()

    opacity: open ? 1 : 0
    visible: opacity > 0

    Behavior on opacity { NumberAnimation { duration: 180; easing.type: Easing.OutCubic } }

    onOpenChanged: {
        if (open)
            Qt.callLater(() => list.positionViewAtEnd())
    }

    onEntriesChanged: Qt.callLater(() => list.positionViewAtEnd())

    Rectangle {
        anchors.fill: parent
        radius: 6
        color: "#cc101014"

        MouseArea {
            anchors.fill: parent
            onClicked: root.closed()
        }
    }

    Rectangle {
        id: sheet
        anchors.left: parent.left
        anchors.right: parent.right
        anchors.bottom: parent.bottom
        anchors.margins: 12
        anchors.bottomMargin: root.open ? 12 : -24
        height: Math.min(parent.height - 24, 440, list.contentHeight + 90)
        radius: 4
        color: "#1b1b20"
        border.width: 1
        border.color: "#2a2a30"

        Behavior on anchors.bottomMargin { NumberAnimation { duration: 240; easing.type: Easing.OutCubic } }

        MouseArea {
            anchors.fill: parent
        }

        ColumnLayout {
            anchors.fill: parent
            anchors.margins: 16
            spacing: 12

            RowLayout {
                Layout.fillWidth: true

                Text {
                    Layout.fillWidth: true
                    text: "Changelog"
                    color: "#e6e6ee"
                    font.pixelSize: 16
                }

                Btn {
                    label: "Close"
                    onClicked: root.closed()
                }
            }

            ListView {
                id: list
                Layout.fillWidth: true
                Layout.fillHeight: true
                clip: true
                spacing: 18
                boundsBehavior: Flickable.StopAtBounds
                model: root.entries

                delegate: Column {
                    id: entry
                    required property var modelData
                    required property int index
                    width: list.width - 14
                    spacing: 6

                    Row {
                        spacing: 8

                        Text {
                            text: entry.modelData.version
                            color: "#7aa2f7"
                            font.pixelSize: 14
                        }

                        Text {
                            visible: entry.index === root.entries.length - 1
                            anchors.verticalCenter: parent.verticalCenter
                            text: "current"
                            color: "#8a8a94"
                            font.pixelSize: 11
                        }
                    }

                    Text {
                        width: parent.width
                        text: entry.modelData.log
                        color: "#c4c4cc"
                        font.pixelSize: 13
                        wrapMode: Text.WordWrap
                        lineHeight: 1.2
                    }
                }
            }
        }
    }
}
