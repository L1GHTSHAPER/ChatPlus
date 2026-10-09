namespace ChatPlus
{
    internal sealed partial class Lang
    {
        public string TabNickname => OutgoingText("Nickname", "Ник");
        public string NicknameTitle => OutgoingText("Nickname style", "Оформление ника");
        public string NicknameEnable => OutgoingText("Style my nickname", "Оформлять мой ник");
        public string NicknameWorld => OutgoingText("Above my character and in Tab", "Над головой и в Tab");
        public string NicknameWorldHint => OutgoingText(
            "Nameplate and player-list changes synchronize after a short pause, including to players without mods. The game also uses this name on ID cards. Turning this option off restores the original world name.",
            "Имя над головой и в списке обновляется после небольшой паузы, в том числе у игроков без модов. Игра использует это имя и в карточке. Выключение этой опции возвращает исходное имя вне чата.");
        public string NicknameHint => OutgoingText(
            "Other players see this style on your new chat messages, even without mods. Your saved name stays the same. Message styling is separate.",
            "Оформление видно другим игрокам в новых сообщениях, даже без модов. Сохранённый ник остаётся прежним. Текст сообщений настраивается отдельно.");
        public string NicknameSample => OutgoingText("My nickname", "Мой ник");
        public string NicknameFallback => OutgoingText(
            "This name cannot fit the selected style. It will be sent with its original formatting.",
            "Ник не помещается с выбранным оформлением. Он будет отправлен в исходном виде.");
    }
}
