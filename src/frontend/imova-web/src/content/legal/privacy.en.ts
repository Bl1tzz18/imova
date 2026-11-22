import type { LegalDocument } from "@/lib/legal/document";

// Privacy policy — English translation of privacy.ro.ts (the Romanian text prevails). Same sections,
// same order.
export const privacyEn: LegalDocument = {
  intro:
    "This policy explains what personal data we process when you use IMOVA, why, how long we keep it and what rights you have. We've kept it as plain as we can: if anything is unclear, write to us.",
  sections: [
    {
      id: "controller",
      heading: "Who we are",
      blocks: [
        "The controller of your data is IMOVA (imova.md), Republic of Moldova (\"IMOVA\", \"we\").",
        "For any question or request about your data, write to [contact@imova.md](mailto:contact@imova.md).",
      ],
    },
    {
      id: "data",
      heading: "What data we process",
      blocks: [
        "We only process what you give us or what comes from using the site:",
        {
          list: [
            "**Your account:** email address, name, phone number, password (stored only in irreversibly hashed form), profile picture, site language and notification preferences. If you sign in with Google, Google gives us your name, email address and profile picture.",
            "**Listings:** the property's details, price, address and map position, photos, and the listing's contact details (yours or those of the contact person you name: name, phone, email, messaging apps and the hours they can be called). We publish another contact person's details on your confirmation that they agreed; that person can ask us to remove them at any time at [contact@imova.md](mailto:contact@imova.md).",
            "**Agencies:** the agency's name, logo, contact details and who its members are; statistics about the agency's listings.",
            "**Messages:** your conversations with other users, including the images sent, and their status (delivered, read).",
            "**Account activity:** saved listings, saved searches and their alerts, the reports you file (on listings or conversations), the users you've blocked.",
            "**Telegram (only if you connect it):** the id of your chat with our bot, your Telegram username and first name.",
            "**Technical data:** your IP address (used on the spot to limit abusive attempts and kept only in technical logs), sign-in sessions (when a session started and ended) and an anonymous visitor id (see [Cookies](/privacy#cookies)).",
          ],
        },
        "We don't ask for or process special categories of data (such as health, religion or political opinions). Please don't put them in listings or messages.",
      ],
    },
    {
      id: "purposes",
      heading: "Why we process it, and on what basis",
      blocks: [
        {
          table: {
            columns: ["Purpose", "Data", "Legal basis"],
            rows: [
              ["Creating and running your account, signing in", "Account, sessions", "Performance of the contract (the [Terms of use](/terms))"],
              ["Publishing and showing listings, agency profiles", "Listings, agencies, photos", "Performance of the contract"],
              ["Messages between users and notifications of new messages", "Messages, account", "Performance of the contract"],
              ["Saved-search alerts (email or Telegram)", "Saved searches, account, Telegram", "Performance of the contract — you turn them on and off"],
              ["Emails about saved listings (price changed, listing ended) and about your listings expiring", "Saved listings, your listings, account", "Legitimate interest in keeping you informed about what you follow; you can unsubscribe at any time"],
              ["Moderating listings, handling reports, preventing fraud and spam, automatically flagging suspicious messages for human review", "Listings, messages, reports, technical data", "Legitimate interest in keeping the platform safe"],
              ["Statistics for listing owners (views, phone reveals, saves)", "Anonymous visitor id, account", "Legitimate interest in accurate statistics without double counting"],
              ["Security: limiting attempts, a log of administrators' actions", "IP address, technical data", "Legitimate interest in protecting accounts and the service"],
              ["Answering requests from authorities", "The data requested, within the law", "Legal obligation"],
            ],
          },
        },
        "We don't make decisions based solely on automated processing that significantly affect you. The automatic filter only flags a message; a person decides.",
        "We don't use your data for advertising, don't build marketing profiles and don't sell data to anyone.",
      ],
    },
    {
      id: "public",
      heading: "What is public",
      blocks: [
        "A published listing can be seen by anyone, without an account, including search engines: its title, description, price, details, photos, map location and the name of whoever published it (a person or an agency, with their profile picture or logo). The listing's phone number only appears after a visitor presses \"Show number\" (and not at all if you've hidden it). The listing's contact email is never shown publicly.",
        "Listing photos and profile pictures are served from public addresses. Images in messages are private and only visible to the people in the conversation.",
      ],
    },
    {
      id: "recipients",
      heading: "Who we share data with",
      blocks: [
        {
          list: [
            "**Other users**, as far as you choose: people who see your listing or whom you write to.",
            "**IMOVA administrators**, who review listings before they're published and handle reports. An administrator only sees a conversation if it was reported or a message in it was automatically flagged.",
            "**Microsoft (Azure)** — hosting of the servers, database and files, and the error reports and technical logs (Azure Monitor), in data centres in the European Union. Error reports keep neither the IP address nor the parameters in page addresses.",
            "**Our email service provider** — sending emails (confirmations, alerts, notifications).",
            "**Google** — only if you choose \"Continue with Google\": Google learns that you're signing in to IMOVA.",
            "**Telegram** — only if you connect your account: alerts are sent through Telegram.",
            "**OpenStreetMap Foundation** — the site's maps load straight from your browser from OpenStreetMap's servers, which see your IP address and the map area shown.",
            "**Address lookup services** (OpenStreetMap's Nominatim, Komoot's Photon) — they only receive the address you type when publishing a listing, not who you are.",
            "**Authorities**, only when the law requires it.",
          ],
        },
      ],
    },
    {
      id: "transfers",
      heading: "Transfers outside the Republic of Moldova",
      blocks: [
        "Data is hosted in the European Union, which ensures an adequate level of protection. The OpenStreetMap Foundation is in the United Kingdom, which has an EU adequacy decision. Google and Telegram may also process data in other countries; we only use them at your request (Google sign-in, connecting Telegram), and the transfer is necessary for the service you asked for. Where providers offer standard contractual clauses, we rely on them.",
      ],
    },
    {
      id: "retention",
      heading: "How long we keep data",
      blocks: [
        {
          table: {
            columns: ["Data", "How long"],
            rows: [
              ["Account and profile", "As long as you have an account. They're gone as soon as you delete it."],
              ["Active, pending, rejected and draft listings", "Until you delete them or your account."],
              ["Ended listings (sold, rented, expired, withdrawn)", "12 months after their last change, then deleted automatically with their photos."],
              ["Photos uploaded for a listing that was never created", "7 days."],
              ["Conversations and their images", "24 months after the last message, then deleted automatically (a reported conversation is kept until the report is resolved)."],
              ["Saved listings and searches, blocks", "Until you delete them or your account."],
              ["Reports you file about a listing", "As long as the reported listing exists."],
              ["Sign-in sessions", "Until they expire (at most 90 days), then deleted."],
              ["The anonymous visitor id (on our servers, only as a cryptographic fingerprint)", "2 days."],
              ["Phone reveals for agency statistics", "400 days."],
              ["CSV files imported by agencies", "30 days."],
              ["The log of administrators' actions", "Permanently; the IP address in it is deleted after 12 months."],
              ["Telegram connection", "Until you disconnect it or delete your account."],
              ["Database backups", "Up to 7 days, then overwritten."],
              ["Server technical logs", "Up to 90 days."],
            ],
          },
        },
      ],
    },
    {
      id: "rights",
      heading: "Your rights",
      blocks: [
        "Under Moldova's Law no. 195/2024 and, if you live in the European Union, the GDPR, you have the right:",
        {
          list: [
            "**of access and to portability** — download a copy of all your data at any time (a JSON file plus your photos and images) from [My account → My data](/account?tab=privacy);",
            "**to rectification** — change your account details and listings at any time;",
            "**to erasure** — delete your account from [My account → My data](/account?tab=privacy). Deletion is immediate and final. Only the messages you sent to people who still have an account remain (they're part of their correspondence too; your name shows as \"Deleted account\"), as do the reports you filed;",
            "**to restriction** and **to object** to processing based on legitimate interest;",
            "**to withdraw your consent** or stop notifications at any time, from your account settings or the unsubscribe link in every email;",
            "**to lodge a complaint** with Moldova's National Centre for Personal Data Protection ([datepersonale.md](https://datepersonale.md)) or, if you live in the EU, with your country's data protection authority.",
          ],
        },
        "For any request you can't make from your account, write to [contact@imova.md](mailto:contact@imova.md). We answer within one month; for complex requests this may be extended by two more months, with an explanation. We may ask you to confirm your identity first.",
      ],
    },
    {
      id: "cookies",
      heading: "Cookies and browser storage",
      blocks: [
        "We only use our own cookies, needed for the site to work. We use no advertising, analytics or third-party cookies — which is why we don't ask for consent with a banner. If we ever add cookies like that, we'll ask for your consent first.",
        {
          table: {
            columns: ["Name", "What it's for", "Duration"],
            rows: [
              ["imova_token", "Keeps you signed in.", "Until you close the browser or, with \"Remember me\", up to 30 days of inactivity (90 at most)."],
              ["imova_refresh", "Renews your sign-in without signing in again.", "Same as imova_token."],
              ["imova_vid", "A random, anonymous id so that a view or a phone reveal on a listing is counted only once a day. It isn't linked to who you are and isn't shared with anyone.", "1 year."],
              ["NEXT_LOCALE", "Remembers the site's language.", "1 year."],
              ["imova_locale_chosen", "Remembers that you picked the language yourself, so it isn't changed when you sign in.", "1 year."],
            ],
          },
        },
        "Browser storage holds a few preferences and lists, with no personal data: whether you publish as a person or an agency (localStorage), your last search for the \"Back\" button and lists of places (sessionStorage, cleared when you close the tab).",
        "On the sign-in pages, the \"Continue with Google\" button is loaded from Google, which may use its own cookies under Google's policy. Maps load from OpenStreetMap, without cookies.",
        "You can delete or block cookies in your browser's settings; without the sign-in cookies you won't be able to stay signed in.",
      ],
    },
    {
      id: "security",
      heading: "How we protect data",
      blocks: [
        "All traffic with the site is encrypted (HTTPS). Passwords are only stored as cryptographic hashes, never in plain text. Sessions expire and can be signed out on every device; changing your password ends the other sessions. Message images are kept in private storage. Administrators' access is limited to what they need for moderation, and sensitive actions are logged.",
        "If a data breach could affect your rights, we'll notify the authority and, where the law requires it, you as well.",
      ],
    },
    {
      id: "minors",
      heading: "Minors",
      blocks: [
        "An IMOVA account is for people aged 18 or over. We don't knowingly collect data about children; if you find that a child has given us data, write to us and we'll delete it.",
      ],
    },
    {
      id: "changes",
      heading: "Changes to this policy",
      blocks: [
        "The date of the last update is shown at the top of the page. If we change something important, we'll email you before the change applies.",
      ],
    },
  ],
};
