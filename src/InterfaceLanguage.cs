namespace ChatPlus
{
    internal sealed partial class Lang
    {
        public string SettingsSubtitle => OutgoingText("Chat settings", "Настройки чата");
        public string TabAppearance => OutgoingText("Appearance", "Вид");
        public string TabMessages => OutgoingText("Messages", "Сообщения");
        public string TabHistory => OutgoingText("History", "История");
        public string TabControls => OutgoingText("Controls", "Управление");
        public string ReadingTitle => OutgoingText("Comfortable reading", "Комфортное чтение");
        public string WindowAdvanced => OutgoingText("Window size and position", "Размер и положение окна");
        public string TimeAdvanced => OutgoingText("Time appearance", "Оформление времени");
        public string AutoSaved => OutgoingText("Changes save automatically", "Изменения сохраняются автоматически");
        public string PreviewLive => OutgoingText("Live preview", "Живой предпросмотр");
        public string PreviewChat => OutgoingText("<b>Lena:</b> Who's joining for 25 minutes of focus?\n<b>Asya:</b> Me! Just making some tea.",
            "<b>Лена:</b> Кто со мной на 25 минут фокуса?\n<b>Ася:</b> Я! Только налью чай.");
        public string ControlsTitle => OutgoingText("Gestures and keys", "Жесты и клавиши");
        public string ScrollGesture => OutgoingText("Scroll the chat", "Прокрутка чата");
        public string SelectGesture => OutgoingText("Select text", "Выделение текста");
        public string CopyGesture => OutgoingText("Copy selection", "Копирование выделенного");
        public string DragKey => OutgoingText("Left mouse + drag", "ЛКМ + движение");
        public string SelectKey => OutgoingText("Shift + left mouse", "Shift + ЛКМ");
        public string SelectionOff => OutgoingText("Text selection is off", "Выделение текста выключено");
        public string ChatScrollHint => OutgoingText("Drag: scroll", "Тянуть: прокрутка");
        public string ChatSelectHint => OutgoingText("Shift + drag: select", "Shift + тянуть: выделение");
        public string ChatCopyHint => OutgoingText("Ctrl+C: copy", "Ctrl+C: копия");
        public string SettingsShort => OutgoingText("Settings", "Настройки");
    }
}
