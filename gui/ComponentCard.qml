import QtQuick
import QtQuick.Layouts

Rectangle {
    id: card

    required property int index
    required property string name
    required property string type
    required property string source
    required property string saveHook
    required property string loadHook
    required property bool saveOn
    required property bool loadOn
    required property bool hypr
    required property bool open
    required property string problem

    property var tab
    property bool touched: false
    property real shown: open ? 1 : 0

    readonly property bool isCopy: type === "copy"
    readonly property bool showProblem: problem !== "" && (touched || problem !== "empty")

    readonly property string problemText: {
        if (problem === "empty")
            return "Give this component a name."
        if (problem === "duplicate")
            return "Another component already has this name."
        if (problem === "invalid")
            return "Names can't contain a slash or start with a dot."
        return ""
    }

    readonly property string summary: {
        if (isCopy) {
            const parts = [source.trim() === "" ? "No file or folder yet" : "Copies " + source.trim()]
            if (saveOn)
                parts.push("runs a command after saving")
            if (loadOn)
                parts.push("runs a command after loading")
            return parts.join("  ·  ")
        }

        if (saveOn && loadOn)
            return "Runs your commands when saving and loading"
        if (saveOn)
            return "Runs a command when saving"
        if (loadOn)
            return "Runs a command when loading"
        return "Nothing set up yet"
    }

    function focusName() {
        nameField.focusInput()
    }

    height: content.implicitHeight + 24
    radius: 4
    color: "#1b1b20"
    border.width: 1
    border.color: "#2a2a30"

    Behavior on shown { NumberAnimation { duration: 220; easing.type: Easing.OutCubic } }

    ColumnLayout {
        id: content
        x: 12
        y: 12
        width: card.width - 24
        spacing: 10

        RowLayout {
            Layout.fillWidth: true
            spacing: 8

            Item {
                Layout.preferredWidth: 20
                Layout.preferredHeight: 30

                Text {
                    anchors.centerIn: parent
                    text: "›"
                    color: chevronHit.containsMouse ? "#e6e6ee" : "#8a8a94"
                    font.pixelSize: 20
                    rotation: card.shown * 90

                    Behavior on color { ColorAnimation { duration: 140 } }
                }

                MouseArea {
                    id: chevronHit
                    anchors.fill: parent
                    hoverEnabled: true
                    cursorShape: Qt.PointingHandCursor
                    onClicked: card.tab.setField(card.index, "open", !card.open)
                }
            }

            Input {
                id: nameField
                Layout.fillWidth: true
                fontSize: 14
                hint: "Name this component, e.g. kitty"
                error: card.showProblem
                onEdited: card.tab.setField(card.index, "name", text)
                onActiveFocusChanged: {
                    if (!activeFocus)
                        card.touched = true
                }
                Component.onCompleted: text = card.name
            }

            Segmented {
                options: ["Copy", "Script"]
                current: card.isCopy ? 0 : 1
                onPicked: index => card.tab.setType(card.index, index === 0 ? "copy" : "script")
            }

            ConfirmBtn {
                idleLabel: "Delete"
                askLabel: "Click again to delete"
                onConfirmed: card.tab.removeAt(card.index)
            }
        }

        Text {
            Layout.fillWidth: true
            Layout.leftMargin: 28
            visible: card.showProblem
            text: card.problemText
            color: "#eba0a6"
            font.pixelSize: 12
            wrapMode: Text.WordWrap
        }

        Muted {
            Layout.fillWidth: true
            Layout.leftMargin: 28
            visible: card.shown < 0.5
            text: card.summary
            elide: Text.ElideRight
        }

        Item {
            Layout.fillWidth: true
            Layout.preferredHeight: body.implicitHeight * card.shown
            visible: card.shown > 0.001
            opacity: card.shown
            clip: true

            ColumnLayout {
                id: body
                width: parent.width
                spacing: 10

                ColumnLayout {
                    Layout.fillWidth: true
                    visible: card.isCopy
                    spacing: 6

                    RowLayout {
                        Layout.fillWidth: true
                        spacing: 8

                        Text {
                            text: "Save the file or folder"
                            color: "#c4c4cc"
                            font.pixelSize: 13
                        }

                        Input {
                            id: sourceField
                            Layout.fillWidth: true
                            hint: "~/.config/kitty"
                            onEdited: card.tab.setField(card.index, "source", text)
                            Component.onCompleted: text = card.source
                        }

                        Text {
                            text: "with every style."
                            color: "#c4c4cc"
                            font.pixelSize: 13
                        }
                    }

                    Muted {
                        Layout.fillWidth: true
                        text: card.source.trim() === ""
                            ? "Nothing to copy yet, so this component is skipped for now."
                            : "Loading a style puts it back right where it was."
                        wrapMode: Text.WordWrap
                    }
                }

                Muted {
                    Layout.fillWidth: true
                    visible: !card.isCopy
                    text: "For apps a plain copy can't handle. You decide what happens when a style is saved and loaded."
                    wrapMode: Text.WordWrap
                }

                HookRow {
                    Layout.fillWidth: true
                    label: card.isCopy ? "After saving, run" : "When saving, run"
                    hint: card.isCopy ? "notify-send \"Saved kitty\"" : "cp ~/.config/myapp/theme \"$STYLE_PATH/theme\""
                    on: card.saveOn
                    value: card.saveHook
                    onSwitched: value => card.tab.setField(card.index, "saveOn", value)
                    onEdited: text => card.tab.setField(card.index, "saveHook", text)
                }

                HookRow {
                    Layout.fillWidth: true
                    label: card.isCopy ? "After loading, run" : "When loading, run"
                    hint: card.isCopy ? "pkill -USR1 kitty" : "cp \"$STYLE_PATH/theme\" ~/.config/myapp/theme"
                    on: card.loadOn
                    value: card.loadHook
                    onSwitched: value => card.tab.setField(card.index, "loadOn", value)
                    onEdited: text => card.tab.setField(card.index, "loadHook", text)
                }

                Muted {
                    Layout.fillWidth: true
                    visible: card.saveOn || card.loadOn
                    text: "In commands, $STYLE_PATH is the style's folder and $COMPONENT is this component's name."
                    wrapMode: Text.WordWrap
                }

                RowLayout {
                    Layout.fillWidth: true
                    visible: card.isCopy
                    spacing: 10

                    Toggle {
                        checked: card.hypr
                        onToggled: value => card.tab.setField(card.index, "hypr", value)
                    }

                    Text {
                        text: "One of Hyprland's own config files"
                        color: card.hypr ? "#c4c4cc" : "#7a7a84"
                        font.pixelSize: 13

                        Behavior on color { ColorAnimation { duration: 140 } }
                    }

                    Muted {
                        Layout.fillWidth: true
                        text: "Loaded first, with Hyprland reloaded before the rest."
                        elide: Text.ElideRight
                        visible: card.hypr
                    }

                    Item {
                        Layout.fillWidth: true
                        visible: !card.hypr
                    }
                }
            }
        }
    }
}
