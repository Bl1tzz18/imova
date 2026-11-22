using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Domain.SavedSearches;
using static Imova.Application.Features.Telegram.Bot.TelegramHtml;

namespace Imova.Application.Features.Telegram.Bot;

// Everything the bot says, in ro/en/ru (Russian uses «вы»). The site's own names are kept —
// "Setările contului → Notificări", "Căutări salvate" — so the steps match what the user sees.
public static class TelegramBotTexts
{
    // The language to answer a chat in: the account's, else the Telegram app's ("ru-RU" → ru), else Romanian.
    public static string LanguageFor(string? accountLanguage, string? telegramLanguageCode)
    {
        if (accountLanguage is not null)
        {
            return Languages.Normalize(accountLanguage);
        }

        var code = telegramLanguageCode?.Split('-', '_')[0].ToLowerInvariant();
        return Languages.Normalize(code);
    }

    public static string SettingsPath(string language) => Languages.Pick(language,
        "Setările contului → Notificări",
        "Account settings → Notifications",
        "Настройки аккаунта → Уведомления");

    public static string OpenSettingsButton(string language) => Languages.Pick(language, "Deschide setările", "Open settings", "Открыть настройки");

    public static string Welcome(string language) => Languages.Pick(language,
        "Bună! Sunt botul <b>IMOVA</b>. Aici poți primi alerte cu anunțurile noi pentru căutările tale salvate.\n\n"
        + $"Ca să-ți conectezi contul, deschide pe site <b>{SettingsPath(language)}</b> și apasă „Conectează Telegram”.",
        "Hi! I'm the <b>IMOVA</b> bot. Here you can get alerts about new listings for your saved searches.\n\n"
        + $"To connect your account, open <b>{SettingsPath(language)}</b> on the site and press “Connect Telegram”.",
        "Здравствуйте! Это бот <b>IMOVA</b>. Здесь можно получать уведомления о новых объявлениях по вашим сохранённым поискам.\n\n"
        + $"Чтобы подключить аккаунт, откройте на сайте <b>{SettingsPath(language)}</b> и нажмите «Подключить Telegram».");

    public static string Commands(string language) => Languages.Pick(language,
        "/cautari — căutările tale salvate\n/stop — deconectează Telegramul",
        "/cautari — your saved searches\n/stop — disconnect Telegram",
        "/cautari — ваши сохранённые поиски\n/stop — отключить Telegram");

    public static string Linked(string language, string email) => Languages.Pick(language,
        $"Gata! Telegramul e conectat la contul IMOVA <b>{Encode(email)}</b>.\n\n"
        + "Aici vor veni alertele pentru căutările tale salvate. Unde le primești alegi pentru fiecare căutare, în „Căutări salvate” pe site.\n\n",
        $"Done! Telegram is now connected to the IMOVA account <b>{Encode(email)}</b>.\n\n"
        + "Alerts for your saved searches will come here. You choose where each search's alerts go under “Saved searches” on the site.\n\n",
        $"Готово! Telegram подключён к аккаунту IMOVA <b>{Encode(email)}</b>.\n\n"
        + "Сюда будут приходить уведомления по вашим сохранённым поискам. Куда присылать уведомления для каждого поиска, вы выбираете в разделе «Сохранённые поиски» на сайте.\n\n")
        + Commands(language);

    public static string AlreadyConnected(string language, string email) => Languages.Pick(language,
        $"Telegramul e conectat la contul IMOVA <b>{Encode(email)}</b>.\n\n",
        $"Telegram is connected to the IMOVA account <b>{Encode(email)}</b>.\n\n",
        $"Telegram подключён к аккаунту IMOVA <b>{Encode(email)}</b>.\n\n")
        + Commands(language);

    public static string Help(string language) => Languages.Pick(language, "Iată ce pot face:\n", "Here's what I can do:\n", "Вот что я умею:\n")
        + Commands(language);

    public static string LinkExpired(string language) => Languages.Pick(language,
        "Linkul de conectare a expirat sau a fost deja folosit. Cere unul nou pe site: "
        + $"<b>{SettingsPath(language)} → „Conectează Telegram”</b>. Un link e valabil 15 minute.",
        "This connection link has expired or was already used. Get a new one on the site: "
        + $"<b>{SettingsPath(language)} → “Connect Telegram”</b>. A link works for 15 minutes.",
        "Ссылка для подключения устарела или уже была использована. Получите новую на сайте: "
        + $"<b>{SettingsPath(language)} → «Подключить Telegram»</b>. Ссылка действует 15 минут.");

    public static string Disconnected(string language) => Languages.Pick(language,
        "Am deconectat Telegramul de la contul IMOVA. Nu-ți mai trimit alerte aici.\n\n"
        + $"Te poți reconecta oricând din {SettingsPath(language)}.",
        "Telegram is now disconnected from your IMOVA account. I won't send you alerts here any more.\n\n"
        + $"You can reconnect anytime from {SettingsPath(language)}.",
        "Telegram отключён от аккаунта IMOVA. Уведомления сюда больше не придут.\n\n"
        + $"Подключить снова можно в любой момент: {SettingsPath(language)}.");

    public static string NotConnected(string language) => Languages.Pick(language,
        "Acest Telegram nu e conectat la niciun cont IMOVA.",
        "This Telegram isn't connected to any IMOVA account.",
        "Этот Telegram не подключён ни к одному аккаунту IMOVA.");

    public static string SavedSearchesHeading(string language) =>
        Languages.Pick(language, "<b>Căutările tale salvate</b>", "<b>Your saved searches</b>", "<b>Ваши сохранённые поиски</b>");

    public static string NoSavedSearches(string language) => Languages.Pick(language,
        "Nu ai încă nicio căutare salvată. Caută pe IMOVA și apasă „Salvează căutarea”.",
        "You don't have any saved searches yet. Search on IMOVA and press “Save this search”.",
        "У вас пока нет сохранённых поисков. Найдите объявления на IMOVA и нажмите «Сохранить поиск».");

    public static string AllSavedSearchesButton(string language) =>
        Languages.Pick(language, "Toate căutările salvate", "All saved searches", "Все сохранённые поиски");

    public static string SearchButton(string language) => Languages.Pick(language, "Caută anunțuri", "Search listings", "Искать объявления");

    // The same words as the frequency picker on /saved-searches (SavedSearches.frequency).
    public static string Frequency(string language, AlertFrequency frequency) => frequency switch
    {
        AlertFrequency.Daily => Languages.Pick(language, "O dată pe zi", "Once a day", "Раз в день"),
        AlertFrequency.Instant => Languages.Pick(language, "Imediat", "Instantly", "Сразу"),
        _ => Languages.Pick(language, "Fără alerte", "No alerts", "Без уведомлений"),
    };

    // The command menu and the texts shown before a chat starts. /start isn't listed: Telegram shows
    // its own Start button, and a plain /start only explains how to connect.
    public static TelegramBotProfile Profile(string language) => new(
        [
            new TelegramBotCommand("cautari", Languages.Pick(language, "Căutările mele salvate", "My saved searches", "Мои сохранённые поиски")),
            new TelegramBotCommand("stop", Languages.Pick(language, "Deconectează Telegramul", "Disconnect Telegram", "Отключить Telegram")),
        ],
        Languages.Pick(language,
            "Alertele IMOVA pe Telegram: anunțurile noi pentru căutările tale salvate, imediat ce apar. "
            + $"Conectează-ți contul pe site, din {SettingsPath(language)}.",
            "IMOVA alerts on Telegram: new listings for your saved searches, as soon as they appear. "
            + $"Connect your account on the site, from {SettingsPath(language)}.",
            "Уведомления IMOVA в Telegram: новые объявления по вашим сохранённым поискам, как только они появляются. "
            + $"Подключите аккаунт на сайте: {SettingsPath(language)}."),
        Languages.Pick(language,
            "Alerte cu anunțurile noi pentru căutările tale salvate pe IMOVA.",
            "New-listing alerts for your saved searches on IMOVA.",
            "Уведомления о новых объявлениях по сохранённым поискам на IMOVA."));
}
