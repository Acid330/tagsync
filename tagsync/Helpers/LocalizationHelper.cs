namespace tagsync.Helpers;

public static class LocalizationHelper
{
    public static readonly Dictionary<string, Dictionary<string, string>> ParameterTranslations = new()
{
    { "width", new() { { "uk", "Ширина" }, { "en", "Width" } } },
    { "height", new() { { "uk", "Висота" }, { "en", "Height" } } },
    { "length", new() { { "uk", "Довжина" }, { "en", "Length" } } },
    { "thickness", new() { { "uk", "Товщина" }, { "en", "Thickness" } } },
    { "color", new() { { "uk", "Колір" }, { "en", "Color" } } },
    { "cooling", new() { { "uk", "Охолодження" }, { "en", "Cooling" } } },
    { "rgb", new() { { "uk", "Підсвітка RGB" }, { "en", "RGB Lighting" } } },
    { "ports", new() { { "uk", "Порти" }, { "en", "Ports" } } },
    { "power_connectors", new() { { "uk", "Розʼєми живлення" }, { "en", "Power Connectors" } } },
    { "release_year", new() { { "uk", "Рік релізу" }, { "en", "Release Year" } } },
    { "manufacturer", new() { { "uk", "Виробник" }, { "en", "Manufacturer" } } },
    { "memory", new() { { "uk", "Обʼєм памʼяті" }, { "en", "Memory" } } },
    { "memory_type", new() { { "uk", "Тип памʼяті" }, { "en", "Memory Type" } } },
    { "memory_bus", new() { { "uk", "Шина памʼяті" }, { "en", "Memory Bus" } } },
    { "chipset", new() { { "uk", "Графічний процесор" }, { "en", "Chipset" } } },
    { "interface", new() { { "uk", "Інтерфейс" }, { "en", "Interface" } } },
    { "warranty", new() { { "uk", "Гарантія" }, { "en", "Warranty" } } },
    { "dlss", new() { { "uk", "DLSS" }, { "en", "DLSS" } } },
    { "ray_tracing", new() { { "uk", "Ray Tracing" }, { "en", "Ray Tracing" } } },
    { "core_clock", new() { { "uk", "Базова частота" }, { "en", "Core Clock" } } },
    { "boost_clock", new() { { "uk", "Boost частота" }, { "en", "Boost Clock" } } },
    { "cuda_cores", new() { { "uk", "CUDA ядра" }, { "en", "CUDA Cores" } } },
    { "cores", new() { { "uk", "Ядра" }, { "en", "Cores" } } },
    { "stream_processors", new() { { "uk", "Потокові процесори" }, { "en", "Stream Processors" } } },
    { "recommended_psu", new() { { "uk", "Рекомендоване БЖ" }, { "en", "Recommended PSU" } } },
    { "tdp", new() { { "uk", "Споживання енергії (TDP)" }, { "en", "TDP" } } },
    { "price", new() { { "uk", "Ціна" }, { "en", "Price" } } },
    { "brand", new() { { "uk", "Бренд" }, { "en", "Brand" } } },
    { "socket", new() { { "uk", "Сокет" }, { "en", "Socket" } } },
    { "integrated_graphics", new() { { "uk", "Вбудована графіка" }, { "en", "Integrated Graphics" } } },
    { "cooler_included", new() { { "uk", "Кулер у комплекті" }, { "en", "Cooler Included" } } },
    { "memory_support", new() { { "uk", "Підтримка памʼяті" }, { "en", "Memory Support" } } },
    { "l3_cache", new() { { "uk", "Кеш L3" }, { "en", "L3 Cache" } } },
    { "threads", new() { { "uk", "Потоки" }, { "en", "Threads" } } },
    { "base_clock", new() { { "uk", "Базова частота" }, { "en", "Base Clock" } } },
    { "type", new() { { "uk", "Тип" }, { "en", "Type" } } },
    { "form_factor", new() { { "uk", "Форм-фактор" }, { "en", "Form Factor" } } },
    { "ecc", new() { { "uk", "Підтримка ECC" }, { "en", "ECC Support" } } },
    { "modules", new() { { "uk", "Кількість модулів" }, { "en", "Modules" } } },
    { "frequency", new() { { "uk", "Частота памʼяті" }, { "en", "Memory Frequency" } } },
    { "cas_latency", new() { { "uk", "Затримка CAS" }, { "en", "CAS Latency" } } },
    { "read_speed", new() { { "uk", "Швидкість читання" }, { "en", "Read Speed" } } },
    { "write_speed", new() { { "uk", "Швидкість запису" }, { "en", "Write Speed" } } },
    { "capacity", new() { { "uk", "Обʼєм" }, { "en", "Capacity" } } },
    { "certification", new() { { "uk", "Сертифікація" }, { "en", "Certification" } } },
    { "modular", new() { { "uk", "Модульний" }, { "en", "Modular" } } },
    { "power", new() { { "uk", "Потужність" }, { "en", "Power" } } },
    { "fan_size", new() { { "uk", "Розмір вентилятора" }, { "en", "Fan Size" } } },
    { "fan_count", new() { { "uk", "Кількість вентиляторів" }, { "en", "Fan Count" } } },
    { "noise_level", new() { { "uk", "Рівень шуму" }, { "en", "Noise Level" } } },
    { "socket_compatibility", new() { { "uk", "Сумісність з сокетами" }, { "en", "Socket Compatibility" } } },
    { "max_gpu_length", new() { { "uk", "Макс. довжина відеокарти" }, { "en", "Max GPU Length" } } },
    { "fans_included", new() { { "uk", "Вентилятори в комплекті" }, { "en", "Fans Included" } } },
    { "fan_support", new() { { "uk", "Підтримка вентиляторів" }, { "en", "Fan Support" } } },
    { "memory_slots", new() { { "uk", "Слоти памʼяті" }, { "en", "Memory Slots" } } },
    { "max_memory", new() { { "uk", "Максимальний обʼєм памʼяті" }, { "en", "Max Memory Capacity" } } },
    { "m2_slots", new() { { "uk", "Слоти M.2" }, { "en", "M.2 Slots" } } },
    { "sata_ports", new() { { "uk", "SATA порти" }, { "en", "SATA Ports" } } },
    { "side_panel", new() { { "uk", "Бічна панель" }, { "en", "Side Panel" } } }
};

    public static readonly Dictionary<string, Dictionary<string, string>> CategoryTranslations = new()
{
    { "gpu", new() { { "uk", "Відеокарти" }, { "en", "Graphics Cards" } } },
    { "cpu", new() { { "uk", "Процесори" }, { "en", "CPU" } } },
    { "motherboard", new() { { "uk", "Материнські плати" }, { "en", "Motherboards" } } },
    { "ram", new() { { "uk", "Оперативна памʼять" }, { "en", "RAM" } } },
    { "storage", new() { { "uk", "Накопичувачі" }, { "en", "Storage" } } },
    { "cooler", new() { { "uk", "Кулери" }, { "en", "Coolers" } } },
    { "psu", new() { { "uk", "Блок живлення" }, { "en", "Power supply" } } },
    { "case", new() { { "uk", "Корпуси" }, { "en", "Cases" } } }
};

    public static readonly Dictionary<string, (string uk, string en)> ValueValueTranslations = new()
    {
        { "Yes", ("Так", "Yes") },
        { "No", ("Ні", "No") },
        { "1 years", ("3 роки", "3 years") },
        { "2 years", ("3 роки", "3 years") },
        { "3 years", ("3 роки", "3 years") },
        { "4 years", ("4 роки", "4 years") },
        { "5 years", ("5 років", "5 years") },
        { "6 years", ("6 років", "6 years") },
        { "7 years", ("7 років", "7 years") },
        { "8 years", ("8 років", "8 years") },
        { "9 years", ("9 років", "9 years") },
        { "10 years", ("10 років", "10 years") }
    };

    public static readonly Dictionary<string, (string uk, string en)> ValueSuffixes = new()
    {
        { "core_clock", ("МГц", "MHz") },
        { "base_clock", ("МГц", "MHz") },
        { "boost_clock", ("МГц", "MHz") },
        { "frequency", ("МГц", "MHz") },
        { "read_speed", ("МБ/с", "MB/s") },
        { "write_speed", ("МБ/с", "MB/s") },
        { "fan_size", ("мм", "mm") },
        { "height", ("мм", "mm") },
        { "length", ("мм", "mm") },
        { "width", ("мм", "mm") },
        { "noise_level", ("дБ", "dB") },
        { "capacity", ("ГБ", "GB") },
        { "power", ("Вт", "W") },
        { "tdp", ("Вт", "W") },
        { "stream_processors", ("", "") },
        { "recommended_psu", ("Вт", "W") },
        { "price", ("₴", "UAH") }

    };

    public static readonly Dictionary<string, string[]> SynonymMap = new()
    {
        ["gpu"] = new[]{"graphics card", "video card", "gpu card", "видеокарта", "відеокарта", "видюха", "відіюха", "вiдео карта",
        "гп", "гпу", "гпу карта", "графіка", "графическая карта", "графічна карта", "графічний адаптер"},
        ["geforce"] = new[] { "gf", "джифорс", "джифорсе", "nvidia geforce", "джифорс", "nvidia" },
        ["amd"] = new[]{
        "radeon", "amd radeon", "амд", "радеон"},
        ["cpu"] = new[]{
        "processor", "процесор", "процессор", "цп", "цпу", "cpu chip", "central processor", "центральний процесор"},
        ["intel"] = new[]{
        "інтел", "интел", "intel core", "intel cpu", "intel processor"},
        ["motherboard"] = new[]{
        "mobo", "материнка", "материнська плата", "материнская плата", "mother board", "mothercard", "сістемна плата", "системная плата" },
        ["ram"] = new[]{
        "ram", "ram memory", "оперативка", "озу", "оперативна память", "оперативная память", "память"},
        ["ssd"] = new[]{
        "ssd", "накопитель", "жесткий диск", "жестяк", "solid state drive", "диск", "диск ssd", "накопичувач", "твердотільний диск"},
        ["hdd"] = new[]{
        "жесткий диск", "жесткий", "hdd", "винчестер", "жорсткий диск", "жорсткий", "хард", "hard disk", "hard drive"},
        ["cooler"] = new[]{
        "cooler", "вентилятор", "охлаждение", "охолодження", "кулер", "fan", "cpu fan", "процесорний кулер"},
        ["case"] = new[]{
        "корпус", "оболочка", "кейс", "кейc", "computer case", "системник", "системний блок", "комп'ютерний корпус"},
        ["psu"] = new[]{
        "блок питания", "power supply", "питание", "psu", "пс", "псу", "живлення", "живлення комп’ютера","питания"},
    };
}
