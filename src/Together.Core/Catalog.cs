namespace Together.Core;

public static class Catalog
{
    public static readonly string[] ExpenseLabels = ["Дорога туда и обратно", "Проживание", "Питание", "Трансферы и местный транспорт", "Развлечения", "Прочее"];
    public static readonly string[] Criteria = ["budget", "travel", "transfers", "kitchen", "crib", "playground", "distance"];
    public static readonly string[] CriterionLabels = ["Бюджет", "Время в пути", "Пересадки", "Кухня", "Детская кроватка", "Детская площадка", "Расстояние до цели"];
    public const string LocalNotice = "Данные хранятся только в этом браузере. Очистка данных сайта может удалить поездки";
    public const string ReviewNotice = "Проверьте расходы после изменения поездки";
    public static string Amenity(string value) => value switch { "yes" => "Есть", "no" => "Нет", _ => "Не указано" };
}
