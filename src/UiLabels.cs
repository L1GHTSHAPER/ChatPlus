using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChatPlus
{
    internal static class UiLabels
    {
        static readonly Dictionary<string,string[]> Names = new Dictionary<string,string[]>
        {
            ["General"]=new[]{"General","Общее"},["Range"]=new[]{"Range","Радиус"},["Appearance"]=new[]{"Appearance","Вид"},["Quality"]=new[]{"Quality","Качество"},
            ["Orbit"]=new[]{"Orbit","Обзор"},["Lens"]=new[]{"Lens","Объектив"},["FreeCamera"]=new[]{"Free camera","Свободная камера"},["Screenshots"]=new[]{"Screenshots","Снимки"},["FreeCamera.Keys"]=new[]{"Movement keys","Клавиши движения"},
            ["Enabled"]=new[]{"Enabled","Включено"},["ToggleKey"]=new[]{"Toggle key","Клавиша переключения"},["VisibilityMode"]=new[]{"Show range","Показывать радиус"},["HighlightPlayersInRange"]=new[]{"Mark nearby players","Отмечать игроков рядом"},
            ["AutoDetectRadius"]=new[]{"Detect radius","Определять радиус"},["Radius"]=new[]{"Radius (m)","Радиус (м)"},["FillColor"]=new[]{"Area color","Цвет области"},["OutlineColor"]=new[]{"Edge color","Цвет границы"},["OutlineWidth"]=new[]{"Edge width (m)","Ширина границы (м)"},["OutlineOnTop"]=new[]{"Edge above objects","Граница поверх объектов"},["PlayerMarkerColor"]=new[]{"Player marker","Метки игроков"},
            ["Segments"]=new[]{"Circle detail","Детализация круга"},["Rings"]=new[]{"Surface samples","Слои поверхности"},["EdgeRefineSteps"]=new[]{"Edge detail","Уточнение границы"},["UpdateInterval"]=new[]{"Update interval (s)","Интервал обновления (с)"},["MaxStepUp"]=new[]{"Max step height (m)","Высота ступени (м)"},["SurfaceOffset"]=new[]{"Surface offset (m)","Отступ от земли (м)"},
            ["MinDistance"]=new[]{"Near limit (m)","Ближний предел (м)"},["MaxDistance"]=new[]{"Far limit (m)","Дальний предел (м)"},["MinPitch"]=new[]{"Lower angle (°)","Нижний угол (°)"},["MaxPitch"]=new[]{"Upper angle (°)","Верхний угол (°)"},["ZoomStepPercent"]=new[]{"Zoom step (%)","Шаг приближения (%)"},["TiltSensitivity"]=new[]{"Tilt sensitivity","Чувствительность наклона"},["CameraCollision"]=new[]{"Camera collisions","Столкновения камеры"},
            ["FieldOfView"]=new[]{"Field of view (0 = game)","Поле зрения (0 = игра)"},["ShiftWheelChangesFov"]=new[]{"Shift + wheel: FOV","Shift + колесо: поле зрения"},["Speed"]=new[]{"Speed (m/s)","Скорость (м/с)"},["FastMultiplier"]=new[]{"Fast multiplier","Ускорение"},["SlowMultiplier"]=new[]{"Slow multiplier","Замедление"},["LookSensitivity"]=new[]{"Look sensitivity","Чувствительность мыши"},["InvertY"]=new[]{"Invert Y","Инверсия Y"},["Smoothing"]=new[]{"Smoothing","Сглаживание"},["HideUiWhileFlying"]=new[]{"Hide UI in flight","Скрывать интерфейс в полёте"},
            ["Forward"]=new[]{"Forward","Вперёд"},["Back"]=new[]{"Back","Назад"},["Left"]=new[]{"Left","Влево"},["Right"]=new[]{"Right","Вправо"},["Up"]=new[]{"Up","Вверх"},["Down"]=new[]{"Down","Вниз"},["Fast"]=new[]{"Fast","Быстрее"},["Slow"]=new[]{"Slow","Медленнее"},["HideUiKey"]=new[]{"Hide UI key","Клавиша скрытия интерфейса"},["HideWorldSpaceUi"]=new[]{"Hide world labels","Скрывать надписи в мире"},["ShowHints"]=new[]{"Show hints","Показывать подсказки"},
            ["MaxVolumePercent"]=new[]{"Volume limit (%)","Предел громкости (%)"},["DefaultVolumePercent"]=new[]{"Default volume (%)","Громкость по умолчанию (%)"},
            ["Always"]=new[]{"Always","Всегда"},["LocalTabOnly"]=new[]{"Local tab","Вкладка «Локальный»"},["WhileTypingLocal"]=new[]{"While typing locally","При вводе локально"}
        };
        internal static string Name(string key) => Names.TryGetValue(key,out var words) ? words[UiEnvironment.Russian?1:0] : Regex.Replace(key,"([a-z])([A-Z])","$1 $2");
    }
}
