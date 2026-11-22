import type { LegalDocument } from "@/lib/legal/document";

// Terms of use — English translation of terms.ro.ts (the Romanian text prevails). Same sections,
// same order.
export const termsEn: LegalDocument = {
  intro:
    "These terms are the agreement between you and IMOVA when you use the site. By creating an account you accept them and the [Privacy policy](/privacy). Please read them.",
  sections: [
    {
      id: "operator",
      heading: "Who we are",
      blocks: [
        "IMOVA (imova.md) is a platform based in the Republic of Moldova. You can write to us at [contact@imova.md](mailto:contact@imova.md).",
      ],
    },
    {
      id: "service",
      heading: "What IMOVA is",
      blocks: [
        "IMOVA is a real estate listings platform: individuals and agencies publish listings for sale and rent, and visitors search them and contact whoever published them.",
        "**IMOVA is not a party to any transaction.** We don't sell, rent or broker properties, don't check ownership, the condition of properties or the identity of the parties, and don't guarantee the information in listings. Any agreement is made directly between users, at their own responsibility.",
        "Using IMOVA is currently free. If we introduce paid services, we'll describe them separately before they apply.",
      ],
    },
    {
      id: "account",
      heading: "Your account",
      blocks: [
        {
          list: [
            "You must be at least 18 to create an account.",
            "Your account details must be real and up to date, and the phone number must be yours or that of the person you represent.",
            "You're responsible for what happens in your account. Keep your password to yourself and tell us if you suspect someone has used your account.",
            "Don't create new accounts to get around a suspension or restriction.",
          ],
        },
      ],
    },
    {
      id: "listings",
      heading: "Listing rules",
      blocks: [
        "By publishing a listing you confirm that:",
        {
          list: [
            "the property exists and you have the right to offer it for sale or rent (you're the owner, have the owner's consent, or are the owner's agent);",
            "the price, property type, area, location and other details are true and current;",
            "the photos are of the property in the listing and you took them or have the right to use them;",
            "if you name another contact person, they have agreed to their name and contact details appearing on the listing;",
            "each property appears only once for the same kind of transaction (no duplicates);",
            "the listing contains no contact details in photos or in the text outside the fields meant for them, no links to other sites, no advertising for other services and no discriminatory conditions (for example by ethnicity, religion, sex or disability).",
          ],
        },
        "Every listing is reviewed by the IMOVA team before it's published. An active listing expires after 6 months; you get an email 7 days before and can renew it. When the property is sold or rented, please mark the listing as such.",
      ],
    },
    {
      id: "agencies",
      heading: "Agencies",
      blocks: [
        "An account can create an agency and invite other members. Listings published on behalf of an agency are the responsibility of the agency and the agent who published them. Whoever creates an agency confirms they have the right to represent it. The \"verified\" badge only means we've confirmed the agency's details; it isn't a guarantee of its services.",
      ],
    },
    {
      id: "messages",
      heading: "Messages",
      blocks: [
        "Messages on IMOVA are for discussing listings. Don't send spam, advertising, threats or illegal content. An automatic filter may flag messages that look like fraud attempts so that an administrator can check them. You can block or report any user.",
      ],
    },
    {
      id: "prohibited",
      heading: "What isn't allowed",
      blocks: [
        {
          list: [
            "fake or misleading listings, or listings for properties that aren't available;",
            "fraud: asking for advance payments, bank details or codes, impersonating someone else;",
            "illegal or offensive content, or content that infringes other people's rights (including copyright in photos);",
            "automated collection of data from the site (scraping), copying listings to other platforms without the publishers' consent, getting around technical limits;",
            "any attempt to disrupt the site or to access accounts and data that aren't yours.",
          ],
        },
      ],
    },
    {
      id: "moderation",
      heading: "Moderation, reports and appeals",
      blocks: [
        "Anyone with an account can report a listing or a conversation. Anyone else — including someone without an account, or an authority — can tell us about illegal content at [contact@imova.md](mailto:contact@imova.md). We review reports carefully but can't promise to act on every one.",
        "If a listing or someone's behaviour breaks these terms, we may reject or suspend the listing, stop the account from sending messages or close the account. **We always tell you why** (in your account or by email), unless the law forbids it.",
        "If you disagree with a decision, write to [contact@imova.md](mailto:contact@imova.md) with the listing number and your explanation. A person will look at the decision again, and you'll get the answer by email.",
      ],
    },
    {
      id: "content",
      heading: "Your content",
      blocks: [
        "The texts and photos you publish remain yours. You give us a non-exclusive, free, worldwide licence to host, show, resize and crop them (for example for thumbnails) on IMOVA, in search results, in email and Telegram alerts and in the previews of shared links. The licence lasts as long as the content is on IMOVA, plus the time it remains in backups.",
        "IMOVA's name, logo and design belong to us. All other content belongs to whoever published it.",
      ],
    },
    {
      id: "liability",
      heading: "Liability",
      blocks: [
        "We provide IMOVA \"as is\". We do our best for the site to work correctly and without interruptions, but can't guarantee it. The information in listings comes from users, who are responsible for it.",
        "To the extent the law allows, we aren't liable for losses arising from transactions between users, from wrong information in listings or from the site being temporarily unavailable. Nothing in these terms limits our liability for intentional acts or gross negligence, or the rights consumer protection law gives you.",
      ],
    },
    {
      id: "safety",
      heading: "Safety tips",
      blocks: [
        {
          list: [
            "Never pay an advance, deposit or \"reservation\" before you've seen the property and checked the documents.",
            "Check ownership (an extract from the Real Estate Register) before signing anything.",
            "Don't send bank details, codes received by SMS or copies of your documents in messages.",
            "If something looks suspicious, report the listing — you'll help protect others too.",
          ],
        },
      ],
    },
    {
      id: "termination",
      heading: "Closing your account",
      blocks: [
        "You can delete your account at any time from [My account → My data](/account?tab=privacy); deletion is immediate. We may suspend or close an account that seriously or repeatedly breaks these terms, with the reasons for the decision.",
      ],
    },
    {
      id: "changes",
      heading: "Changes to the terms",
      blocks: [
        "We may update these terms. If we change something important, we'll email you before the change applies and, where needed, ask you to accept the new version. The date of the last update is shown at the top of the page.",
      ],
    },
    {
      id: "law",
      heading: "Governing law and disputes",
      blocks: [
        "These terms are governed by the law of the Republic of Moldova. We try to settle any disagreement amicably — please write to us first. If we can't, the dispute will be settled by the competent courts of the Republic of Moldova. If you're a consumer living in the European Union, you keep the protection given by the mandatory rules of your country.",
        "These terms are available in Romanian, Russian and English. If the versions differ, the Romanian one prevails.",
      ],
    },
  ],
};
