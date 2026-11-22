import type { LegalDocument } from "@/lib/legal/document";

// Termenii de utilizare — versiunea în limba română (cea care prevalează). Aceleași secțiuni, în
// aceeași ordine, în en și ru.
export const termsRo: LegalDocument = {
  intro:
    "Acești termeni sunt contractul dintre tine și IMOVA când folosești site-ul. Creându-ți un cont, accepți acești termeni și [Politica de confidențialitate](/privacy). Te rugăm să-i citești.",
  sections: [
    {
      id: "operator",
      heading: "Cine suntem",
      blocks: [
        "IMOVA (imova.md) este o platformă din Republica Moldova. Ne poți scrie la [contact@imova.md](mailto:contact@imova.md).",
      ],
    },
    {
      id: "service",
      heading: "Ce este IMOVA",
      blocks: [
        "IMOVA este o platformă de anunțuri imobiliare: persoane fizice și agenții publică anunțuri de vânzare și de închiriere, iar vizitatorii le caută și îi contactează pe cei care le-au publicat.",
        "**IMOVA nu este parte la tranzacții.** Nu vindem, nu închiriem și nu intermediem proprietăți, nu verificăm dreptul de proprietate, starea imobilelor sau identitatea părților și nu garantăm informațiile din anunțuri. Orice înțelegere se face direct între utilizatori, pe răspunderea lor.",
        "Folosirea IMOVA este în prezent gratuită. Dacă vom introduce servicii plătite, le vom descrie separat, înainte ca ele să se aplice.",
      ],
    },
    {
      id: "account",
      heading: "Contul tău",
      blocks: [
        {
          list: [
            "Trebuie să ai cel puțin 18 ani ca să-ți creezi un cont.",
            "Datele din cont trebuie să fie reale și actuale, iar numărul de telefon trebuie să fie al tău sau al persoanei pe care o reprezinți.",
            "Ești responsabil de ce se întâmplă în contul tău. Păstrează parola doar pentru tine și anunță-ne dacă bănuiești că cineva ți-a folosit contul.",
            "Nu crea conturi noi ca să ocolești o suspendare sau o restricție.",
          ],
        },
      ],
    },
    {
      id: "listings",
      heading: "Reguli pentru anunțuri",
      blocks: [
        "Publicând un anunț, confirmi că:",
        {
          list: [
            "proprietatea există și ai dreptul să o oferi spre vânzare sau închiriere (ești proprietarul, ai acordul lui sau ești agentul lui);",
            "prețul, tipul proprietății, suprafața, localizarea și celelalte detalii sunt adevărate și actuale;",
            "fotografiile sunt ale proprietății din anunț și le-ai făcut tu sau ai dreptul să le folosești;",
            "dacă indici o altă persoană de contact, aceasta a fost de acord ca numele și datele ei de contact să apară pe anunț;",
            "fiecare proprietate apare o singură dată pentru același tip de tranzacție (fără dubluri);",
            "anunțul nu conține date de contact în fotografii sau în text în afara câmpurilor dedicate, linkuri spre alte site-uri, publicitate pentru alte servicii sau condiții discriminatorii (de exemplu după etnie, religie, sex sau dizabilitate).",
          ],
        },
        "Fiecare anunț este verificat de echipa IMOVA înainte de publicare. Un anunț activ expiră după 6 luni; primești un email cu 7 zile înainte și îl poți prelungi. Când proprietatea e vândută sau închiriată, te rugăm să marchezi anunțul ca atare.",
      ],
    },
    {
      id: "agencies",
      heading: "Agenții",
      blocks: [
        "Un cont își poate crea o agenție și poate invita alți membri. Anunțurile publicate în numele unei agenții sunt responsabilitatea agenției și a agentului care le-a publicat. Cine creează o agenție confirmă că are dreptul să o reprezinte. Insigna „verificată” înseamnă doar că am confirmat datele agenției; nu este o garanție a serviciilor ei.",
      ],
    },
    {
      id: "messages",
      heading: "Mesajele",
      blocks: [
        "Mesajele de pe IMOVA servesc la discuții despre anunțuri. Nu trimite spam, reclame, amenințări sau conținut ilegal. Un filtru automat poate semnala mesajele care seamănă cu tentative de fraudă, ca un administrator să le verifice. Poți bloca sau raporta orice utilizator.",
      ],
    },
    {
      id: "prohibited",
      heading: "Ce nu este permis",
      blocks: [
        {
          list: [
            "anunțuri false, înșelătoare sau pentru proprietăți care nu sunt disponibile;",
            "fraude: cereri de plată în avans, de date bancare sau de coduri, uzurparea identității altcuiva;",
            "conținut ilegal, ofensator sau care încalcă drepturile altora (inclusiv drepturile de autor asupra fotografiilor);",
            "colectarea automată a datelor de pe site (scraping), copierea anunțurilor pe alte platforme fără acordul celor care le-au publicat, ocolirea limitelor tehnice;",
            "orice încercare de a perturba funcționarea site-ului sau de a accesa conturi și date care nu îți aparțin.",
          ],
        },
      ],
    },
    {
      id: "moderation",
      heading: "Moderare, raportări și contestații",
      blocks: [
        "Oricine are cont poate raporta un anunț sau o conversație. Oricine altcineva — inclusiv cineva fără cont sau o autoritate — ne poate semnala conținut ilegal la [contact@imova.md](mailto:contact@imova.md). Analizăm raportările cu atenție, dar nu putem garanta că acționăm asupra fiecăreia.",
        "Dacă un anunț sau un comportament încalcă acești termeni, putem respinge sau suspenda anunțul, putem interzice trimiterea de mesaje sau putem închide contul. **Îți comunicăm întotdeauna motivul** (în contul tău sau prin email), cu excepția cazurilor în care legea ne-o interzice.",
        "Dacă nu ești de acord cu o decizie, scrie-ne la [contact@imova.md](mailto:contact@imova.md) cu numărul anunțului și explicația ta. Decizia va fi reanalizată de un om, iar răspunsul îl primești prin email.",
      ],
    },
    {
      id: "content",
      heading: "Conținutul tău",
      blocks: [
        "Textele și fotografiile pe care le publici rămân ale tale. Ne dai o licență neexclusivă, gratuită, pentru toată lumea, de a le găzdui, afișa, redimensiona și decupa (de exemplu pentru miniaturi) pe IMOVA, în rezultatele căutărilor, în alertele prin email și Telegram și în previzualizările linkurilor distribuite. Licența durează cât timp conținutul este pe IMOVA, plus perioada în care se mai află în copiile de siguranță.",
        "Numele, sigla și designul IMOVA ne aparțin. Restul conținutului aparține celor care l-au publicat.",
      ],
    },
    {
      id: "liability",
      heading: "Răspundere",
      blocks: [
        "Oferim IMOVA „așa cum este”. Facem tot posibilul ca site-ul să funcționeze corect și fără întreruperi, dar nu putem garanta acest lucru. Informațiile din anunțuri sunt furnizate de utilizatori, care răspund pentru ele.",
        "În limitele permise de lege, nu răspundem pentru pierderile rezultate din tranzacțiile dintre utilizatori, din informații greșite din anunțuri sau din indisponibilitatea temporară a site-ului. Nimic din acești termeni nu limitează răspunderea noastră pentru fapte săvârșite cu intenție sau din culpă gravă și nici drepturile pe care ți le dă legislația privind protecția consumatorilor.",
      ],
    },
    {
      id: "safety",
      heading: "Sfaturi de siguranță",
      blocks: [
        {
          list: [
            "Nu plăti niciodată avans, garanție sau „rezervare” înainte de a vedea proprietatea și de a verifica actele.",
            "Verifică dreptul de proprietate (extrasul din Registrul bunurilor imobile) înainte de a semna ceva.",
            "Nu trimite date bancare, coduri primite prin SMS sau copii ale actelor prin mesaje.",
            "Dacă ceva pare suspect, raportează anunțul — ne ajuți să-i protejăm și pe ceilalți.",
          ],
        },
      ],
    },
    {
      id: "termination",
      heading: "Închiderea contului",
      blocks: [
        "Îți poți șterge contul oricând, din [Contul meu → Datele mele](/account?tab=privacy); ștergerea este imediată. Putem suspenda sau închide un cont care încalcă grav sau repetat acești termeni, cu motivarea deciziei.",
      ],
    },
    {
      id: "changes",
      heading: "Modificarea termenilor",
      blocks: [
        "Putem actualiza acești termeni. Dacă schimbăm ceva important, te anunțăm prin email înainte ca modificarea să se aplice și, când e cazul, îți cerem să accepți noua versiune. Data ultimei actualizări este afișată în partea de sus a paginii.",
      ],
    },
    {
      id: "law",
      heading: "Legea aplicabilă și litigii",
      blocks: [
        "Acești termeni sunt guvernați de legislația Republicii Moldova. Încercăm să rezolvăm orice neînțelegere pe cale amiabilă — scrie-ne întâi. Dacă nu reușim, litigiul se soluționează de instanțele competente din Republica Moldova. Dacă ești consumator și locuiești în Uniunea Europeană, îți păstrezi protecția oferită de normele obligatorii din țara ta.",
        "Acești termeni sunt disponibili în română, rusă și engleză. În caz de neconcordanță, prevalează versiunea în limba română.",
      ],
    },
  ],
};
