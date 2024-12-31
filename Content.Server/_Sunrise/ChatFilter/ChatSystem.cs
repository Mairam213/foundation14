using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    private static readonly Dictionary<string, string> SlangReplace = new()
    {
        // Game
        { "геник", "генератор" },
        { "кк", "красный код" },
        { "ск", "синий код" },
        { "зк", "зелёный код" },
        { "пда", "кпк" },
        { "корп", "корпоративный" },
        { "дэк", "детектив" },
        { "дэку", "детективу" },
        { "дэка", "детектива" },
        { "дек", "детектив" },
        { "деку", "детективу" },
        { "дека", "детектива" },
        { "мш", "имплант защиты разума" },
        { "трейтор", "предатель" },
        { "инж", "инженер" },
        { "инжи", "инженеры" },
        { "инжы", "инженеры" },
        { "инжу", "инженеру" },
        { "инжам", "инженерам" },
        { "инжинер", "инженер" },
        { "нюк", "ядерный оперативник" },
        { "нюкеры", "ядерные оперативники" },
        { "нюкер", "ядерный оперативник" },
        { "нюкеровец", "ядерный оперативник" },
        { "нюкеров", "ядерных оперативников" },
        { "аирлок", "шлюз" },
        { "аирлоки", "шлюзы" },
        { "айрлок", "шлюз" },
        { "айрлоки", "шлюзы" },
        { "визард", "волшебник" },
        { "дизарм", "толчок" },
        { "синга", "сингулярность" },
        { "сингу", "сингулярность" },
        { "синги", "сингулярности" },
        { "разгерм", "разгерметизация" },
        { "бикардин", "бикаридин" },
        { "бика", "бикаридин" },
        { "бику", "бикаридин" },
        { "декс", "дексалин" },
        { "декса", "дексалин" },
        { "дексу", "дексалин" },
        // IC
        { "норм", "нормально" },
        { "хз", "не знаю" },
        { "синд", "синдикат" },
        { "пон", "понятно" },
        { "непон", "не понятно" },
        { "нипон", "не понятно" },
        { "кста", "кстати" },
        { "кст", "кстати" },
        { "плз", "пожалуйста" },
        { "пж", "пожалуйста" },
        { "спс", "спасибо" },
        { "сяб", "спасибо" },
        { "прив", "привет" },
        { "ок", "окей" },
        { "чел", "мужик" },
        { "лан", "ладно" },
        { "збс", "заебись" },
        { "мб", "может быть" },
        { "оч", "очень" },
        { "омг", "боже мой" },
        { "нзч", "не за что" },
        { "пок", "пока" },
        { "бб", "пока" },
        { "пох", "плевать" },
        { "ясн", "ясно" },
        { "всм", "всмысле" },
        { "чзх", "что за херня?" },
        { "изи", "легко" },
        { "гг", "хорошо сработано" },
        { "пруф", "доказательство" },
        { "пруфани", "докажи" },
        { "пруфанул", "доказал" },
        { "брух", "мда..." },
        { "имба", "нечестно" },
        { "разлокать", "разблокировать" },
        { "юзать", "использовать" },
        { "юзай", "используй" },
        { "юзнул", "использовал" },
        { "хилл", "лечение" },
        { "подхиль", "полечи" },
        { "хильни", "полечи" },
        { "хелп", "помоги" },
        { "хелпани", "помоги" },
        { "хелпанул", "помог" },
        { "рофл", "прикол" },
        { "рофлишь", "шутишь" },
        { "крч", "короче говоря" },
        { "шатл", "шаттл" },
        // OOC
        { "афк", "ссд" },
        { "админ", "бог" },
        { "админы", "боги" },
        { "админов", "богов" },
        { "забанят", "покарают" },
        { "бан", "наказание" },
        { "пермач", "наказание" },
        { "перм", "наказание" },
        { "запермили", "наказание" },
        { "запермят", "накажут" },
        { "нонрп", "плохо" },
        { "нрп", "плохо" },
        { "рдм", "плохо" },
        { "дм", "плохо" },
        { "гриф", "плохо" },
        { "фрикил", "плохо" },
        { "фрикилл", "плохо" },
        { "лкм", "левая рука" },
        { "пкм", "правая рука" },
        // Twitch-friendly
        {"хохол","человек"},
        {"хохолом","человеком"},
        {"хохла", "человека"},
        {"хохлу", "человеку"},
        {"хохлов", "людей"},
        {"хохлы", "люди"},
        {"хач","бородач"},
        {"хача","бородача"},
        {"хачу", "бородачу"},
        {"хачи","бородачи"},
        {"хачик","бородач"},
        {"жид","человек"},
        {"жиду","человеку"},
        {"жида","человека"},
        {"жидами","людьми"},
        {"жиды","люди"},
        {"жидом","человеком"},
        {"nigger","афроамериканец"},
        {"niger","афроамериканец"},
        {"niga","афроамериканец"},
        {"nigga","афроамериканец"},
        {"naga","человек"},
        {"нигер","афроамериканец"},
        {"ниггер","афроамериканец"},
        {"нигеры","афроамериканцы"},
        {"ниггеры","афроамериканцы"},
        {"ниггера","афроамериканца"},
        {"ниггеру","афроамериканцу"},
        {"нигером","афроамериканцем"},
        {"ниггорм","афроамериканцем"},
        {"нигера","афроамериканца"},
        {"нигеру","афроамериканцу"},
        {"нига","афроамериканец"},
        {"нигга","афроамериканец"},
        {"faggot","мужеложец"},
        {"fagot","мужеложец"},
        {"fag","мужеложец"},
        {"fagg","мужеложец"},
        {"пидор","мужеложец"},
        {"пидар","мужеложец"},
        {"пидоры","мужеложцы"},
        {"пидары","мужеложцы"},
        {"пидора","мужеложца"},
        {"пидара","мужеложца"},
        {"пидору","мужеложцу"},
        {"пидару","мужеложцу"},
        {"пидарам","мужеложцам"},
        {"пидором","мужеложцам"},
        {"пидорас","мужеложец"},
        {"пидарас","мужеложец"},
        {"пидорасу","мужеложцу"},
        {"пидарасу","мужеложцу"},
        {"пидораса","мужеложца"},
        {"пидараса","мужеложца"},
        {"пидорасы","мужеложцы"},
        {"пидарасы","мужеложцы"},
        {"пидорасов","мужеложцев"},
        {"пидарасов","мужеложцев"},
        {"черномазый","афроамериканец"},
        {"черномазых","афроамериканец"},
        {"черномазого","афроамериканец"},
        {"негритос","афроамериканец"},
        {"негритоса","афроамериканца"},
        {"негритосов","афроамериканцев"},
        {"русня","люди"},
        {"пидрила","мужеложец"},
        {"педрила","мужеложец"},
        {"пидрилы","мужеложцы"},
        {"педрилы","мужеложцы"},
        {"педераст","мужеложец"},
        {"пидераст","мужеложец"},
        {"педираст","мужеложец"},
        {"гомик","мужеложец"},
        {"гомики","мужеложцы"},
        {"гомиков","мужеложцев"},
        {"гомикав","мужеложцев"},
        {"гомосек","мужеложец"},
        {"гомосеки","мужеложцы"},
        {"гомосеков","мужеложцев"},
        {"гомосятина","мужеложец"},
        {"педик","мужеложец"},
        {"педики","мужеложцы"},
        {"педика","мужеложца"},
        {"педиков","мужеложцев"},
        {"педикав","мужеложцев"},
        {"хиджаб","капюшон"},
        {"паранжа","капюшон"},
        {"даун","нездоровый"},
        {"дауны","нездоровые"},
        {"даунов","нездоровые"},
        {"аутист","нездоровый"},
        {"аутисты","нездоровые"},
        {"аутистов","нездоровые"},
        {"retard","глупый"},
        {"retards","глупый"},
        {"ретард","глупый"},
        {"ретарды","глупые"},
        {"ретардов","глупых"},
        {"virgin","невинный"},
        {"девственник","нецелованный"},
        {"девственники","нецелованныу"},
        {"девственников","нецелованных"},
        {"девственик","нецелованный"},
        {"девственики","нецелованные"},
        {"девствеников","нецелованных"},
        {"simp","человек"},
        {"cимп","человек"},
        {"cимпа","человека"},
        {"cимпу","человеку"},
        {"incel","человек"},
        {"инцел","человек"},
        {"cunt","плохишь"},
        {"куколд","нецелованный"},
        {"куколда","нецелованного"},
        {"куколду","нецелованному"},
        {"куколдам","нецелованным"},
        {"куколды","нецелованные"},
        {"куколдом","нецелованным"},
        {"чурка","бородач"},
        {"чурку","бородачу"},
        {"чурок","бородачи"},
        {"чурки","бородачи"},
        {"чуркой","бородачём"},
        {"жидяра","бородач"},
        {"жидяры","бородачи"},
        {"пиндос","нехороший человек"},
        {"пендос","нехороший человек"},
        {"пиндосом","нехорошим человеком"},
        {"пендосом","нехорошим человеком"},
        {"пендосы","нехорошие люди"},
        {"пиндосы","нехорошие люди"},
        // VigersRay момент
        {"вигерс","Господь"},
        {"вигерсрей","Господь"},
        {"виджерс","Господь"},
        {"vigers","Господь"},
        {"нигерс","Господь"},
        {"хуигерс","Господь"},
        {"vigers ray","Господь"},
        {"vigersray","Господь"},
        {"вига","Господь"},
        // Я вам блядь дам гойду.
        {"гойда","Ура"},
        // Прогресивная регресия
        {"нио","РНД"},
        {"крс","ССД"},
        {"кеерсе","ССД"},
        {"нр","РД"},
        {"енер","РД"},
        {"си","СЕ"},
        {"еси","СЕ"},
        {"гсб","ХоС"},
        {"геесбе","ХоС"},
        {"гп","ХоП"},
        {"гепе","ХоП"},
        {"гв","СМО"},
        {"геве","СМО"},
        // Fire edit start
        {"нус","нарушение условий содержания"},
        {"н.у.с","нарушение условий содержания"},
        {"н.у.с.","нарушение условий содержания"},
        // Fire edit end
    };

    public string ReplaceWords(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        return Regex.Replace(message,
            @"\b(\w+)\b",
            match =>
        {
            var word = match.Value;
            var isUpperCase = word.All(Char.IsUpper);
            var isCapitalized = char.IsUpper(word[0]) && word.Skip(1).All(char.IsLower);

            if (SlangReplace.TryGetValue(match.Value.ToLower(), out var replacement))
            {
                if (isUpperCase)
                    return replacement.ToUpper();
                if (isCapitalized)
                    return char.ToUpper(replacement[0]) + replacement.Substring(1);
                return replacement;
            }
            return word;
        });
    }
}
