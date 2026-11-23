import type { LegalDocument } from "@/lib/legal/document";

// Politica de confidențialitate — versiunea în limba română (cea care prevalează). Legea nr. 195/2024
// privind protecția datelor cu caracter personal (în vigoare din 23.08.2026) și, pentru utilizatorii
// din UE, Regulamentul (UE) 2016/679 (GDPR). Aceleași secțiuni, în aceeași ordine, în en și ru.
export const privacyRo: LegalDocument = {
  intro:
    "Această politică explică ce date personale prelucrăm când folosești IMOVA, de ce, cât timp le păstrăm și ce drepturi ai. Am scris-o cât mai simplu: dacă ceva nu e clar, scrie-ne.",
  sections: [
    {
      id: "controller",
      heading: "Cine suntem",
      blocks: [
        "Operatorul datelor tale este IMOVA (imova.md), Republica Moldova („IMOVA”, „noi”).",
        "Pentru orice întrebare sau cerere privind datele tale ne poți scrie la [contact@imova.md](mailto:contact@imova.md).",
      ],
    },
    {
      id: "data",
      heading: "Ce date prelucrăm",
      blocks: [
        "Prelucrăm doar ce ne dai tu sau ce rezultă din folosirea site-ului:",
        {
          list: [
            "**Contul:** adresa de email, numele, numărul de telefon, parola (stocată doar sub formă criptată, ireversibil), poza de profil, limba site-ului și preferințele de notificare. Dacă intri cu Google, primim de la Google numele, adresa de email și poza de profil.",
            "**Anunțurile:** detaliile proprietății, prețul, adresa și punctul de pe hartă, fotografiile și datele de contact pentru anunț (ale tale sau ale persoanei de contact pe care o indici, cu numele, telefonul, emailul, aplicațiile de mesagerie și orele la care poate fi sunată). Datele unei alte persoane de contact le publicăm pe baza confirmării tale că ea a fost de acord; persoana ne poate cere oricând ștergerea lor la [contact@imova.md](mailto:contact@imova.md).",
            "**Agențiile:** numele, sigla, datele de contact ale agenției și cine sunt membrii ei; statistici despre anunțurile agenției.",
            "**Mesajele:** conversațiile cu alți utilizatori, inclusiv imaginile trimise, și starea lor (livrat, citit).",
            "**Activitatea în cont:** anunțurile salvate, căutările salvate și alertele lor, raportările pe care le faci (anunțuri sau conversații), utilizatorii pe care i-ai blocat.",
            "**Telegram (doar dacă îl conectezi):** identificatorul conversației cu botul nostru, numele de utilizator și prenumele din Telegram.",
            "**Date tehnice:** adresa IP (folosită pe loc pentru limitarea încercărilor abuzive și păstrată doar în jurnalele tehnice), sesiunile de autentificare (dispozitiv conectat, începutul și sfârșitul sesiunii) și un identificator anonim de vizitator (vezi [Cookie-uri](/privacy#cookies)).",
          ],
        },
        "Nu cerem și nu prelucrăm categorii speciale de date (de exemplu date despre sănătate, religie sau opinii politice). Te rugăm să nu le incluzi în anunțuri sau mesaje.",
      ],
    },
    {
      id: "purposes",
      heading: "De ce le prelucrăm și pe ce temei",
      blocks: [
        {
          table: {
            columns: ["Scop", "Date", "Temei juridic"],
            rows: [
              ["Crearea și administrarea contului, autentificarea", "Contul, sesiunile", "Executarea contractului ([Termenii de utilizare](/terms))"],
              ["Publicarea și afișarea anunțurilor, profilul agenției", "Anunțurile, agențiile, fotografiile", "Executarea contractului"],
              ["Mesajele între utilizatori și notificările despre mesaje noi", "Mesajele, contul", "Executarea contractului"],
              ["Alertele pentru căutări salvate (email sau Telegram)", "Căutările salvate, contul, Telegram", "Executarea contractului — le pornești și le oprești tu"],
              ["Emailuri despre anunțurile salvate (preț schimbat, anunț încheiat) și despre expirarea anunțurilor tale", "Anunțurile salvate, anunțurile tale, contul", "Interesul legitim de a te informa despre ce urmărești; te poți dezabona oricând"],
              ["Moderarea anunțurilor, tratarea raportărilor, prevenirea fraudelor și a spamului, semnalarea automată a mesajelor suspecte pentru verificare umană", "Anunțurile, mesajele, raportările, datele tehnice", "Interesul legitim de a ține platforma sigură"],
              ["Statistici pentru proprietarii de anunțuri (vizualizări, afișări ale telefonului, salvări)", "Identificatorul anonim de vizitator, contul", "Interesul legitim de a oferi statistici corecte, fără dubluri"],
              ["Securitate: limitarea încercărilor, jurnalul acțiunilor administratorilor", "Adresa IP, datele tehnice", "Interesul legitim de a proteja conturile și serviciul"],
              ["Răspunsul la cererile autorităților", "Datele cerute, în limita legii", "Obligația legală"],
            ],
          },
        },
        "Nu luăm decizii bazate exclusiv pe prelucrare automată care să te afecteze în mod semnificativ. Filtrul automat doar semnalează un mesaj; decizia o ia un om.",
        "Nu folosim datele tale pentru publicitate, nu facem profiluri de marketing și nu vindem date nimănui.",
      ],
    },
    {
      id: "public",
      heading: "Ce este public",
      blocks: [
        "Un anunț publicat este vizibil oricui, fără cont, inclusiv motoarelor de căutare: titlul, descrierea, prețul, detaliile, fotografiile, localizarea pe hartă și numele celui care publică (persoană sau agenție, cu poza de profil sau sigla). Numărul de telefon pentru anunț apare doar după ce vizitatorul apasă „Arată numărul” (și deloc, dacă l-ai ascuns). Emailul de contact al anunțului nu este afișat public.",
        "Fotografiile anunțurilor și pozele de profil sunt servite prin adrese publice. Imaginile din mesaje sunt private și se văd doar de participanții la conversație.",
      ],
    },
    {
      id: "recipients",
      heading: "Cui transmitem date",
      blocks: [
        {
          list: [
            "**Altor utilizatori**, în măsura în care alegi tu: celor care îți văd anunțul sau cu care corespondezi.",
            "**Administratorilor IMOVA**, care verifică anunțurile înainte de publicare și tratează raportările. O conversație este văzută de un administrator doar dacă a fost raportată sau dacă un mesaj din ea a fost semnalat automat.",
            "**Microsoft (Azure)** — găzduirea serverelor, a bazei de date și a fișierelor, precum și rapoartele de erori și jurnalele tehnice (Azure Monitor), în centre de date din Uniunea Europeană. Rapoartele de erori nu păstrează adresa IP și nici parametrii din adresele paginilor.",
            "**Microsoft (Azure Communication Services)** — trimiterea emailurilor (confirmări, alerte, notificări), în Uniunea Europeană.",
            "**Zoho (Zoho Mail, centre de date din Uniunea Europeană)** — căsuța de email pentru mesajele pe care ni le trimiți la adresele noastre @imova.md (de exemplu contact@imova.md).",
            "**Google** — doar dacă alegi „Continuă cu Google”: Google află că te autentifici la IMOVA.",
            "**Telegram** — doar dacă îți conectezi contul: alertele sunt trimise prin Telegram.",
            "**OpenStreetMap Foundation** — hărțile de pe site se încarcă direct din browserul tău de pe serverele OpenStreetMap, care văd adresa IP și zona de hartă afișată.",
            "**Serviciile de căutare a adreselor** (Nominatim al OpenStreetMap, Photon al Komoot) — primesc doar adresa pe care o tastezi când publici un anunț, nu și cine ești.",
            "**Autorităților**, numai când legea ne obligă.",
          ],
        },
      ],
    },
    {
      id: "transfers",
      heading: "Transferuri în afara Republicii Moldova",
      blocks: [
        "Datele sunt găzduite în Uniunea Europeană, care asigură un nivel de protecție adecvat. OpenStreetMap Foundation se află în Regatul Unit, care beneficiază de o decizie de adecvare a Uniunii Europene. Google și Telegram pot prelucra date și în alte țări; le folosim doar la cererea ta (autentificarea cu Google, conectarea Telegram), iar transferul este necesar pentru serviciul cerut. Acolo unde furnizorii oferă clauze contractuale standard, ne bazăm pe acestea.",
      ],
    },
    {
      id: "retention",
      heading: "Cât timp păstrăm datele",
      blocks: [
        {
          table: {
            columns: ["Date", "Cât timp"],
            rows: [
              ["Contul și profilul", "Cât timp ai cont. La ștergerea contului dispar imediat."],
              ["Anunțurile active, în așteptare, respinse sau ciorne", "Până le ștergi tu sau până ștergi contul."],
              ["Anunțurile încheiate (vândute, închiriate, expirate, retrase)", "12 luni de la ultima modificare, apoi sunt șterse automat, cu fotografiile lor."],
              ["Fotografii încărcate pentru un anunț care nu a mai fost creat", "7 zile."],
              ["Conversațiile și imaginile din ele", "24 de luni de la ultimul mesaj, apoi sunt șterse automat (o conversație raportată se păstrează până la soluționarea raportării)."],
              ["Emailurile trimise la adresele noastre @imova.md", "Cât timp e nevoie pentru a-ți răspunde și pentru eventuale urmări (de exemplu o reclamație sau o cerere legată de datele tale)."],
              ["Anunțurile și căutările salvate, blocările", "Până le ștergi tu sau până ștergi contul."],
              ["Raportările pe care le faci despre un anunț", "Cât timp există anunțul raportat."],
              ["Sesiunile de autentificare", "Până expiră (cel mult 90 de zile), apoi sunt șterse."],
              ["Identificatorul anonim de vizitator (pe server, doar sub formă de amprentă criptografică)", "2 zile."],
              ["Afișările telefonului pentru statisticile agențiilor", "400 de zile."],
              ["Fișierele CSV importate de agenții", "30 de zile."],
              ["Jurnalul acțiunilor administratorilor", "Permanent; adresa IP din el se șterge după 12 luni."],
              ["Conexiunea Telegram", "Până o deconectezi sau ștergi contul."],
              ["Copiile de siguranță ale bazei de date și fișierele șterse (fotografii, imagini din mesaje)", "Până la 7 zile, apoi sunt șterse definitiv."],
              ["Jurnalele tehnice ale serverelor", "Până la 90 de zile."],
            ],
          },
        },
      ],
    },
    {
      id: "rights",
      heading: "Drepturile tale",
      blocks: [
        "Conform Legii nr. 195/2024 și, dacă locuiești în Uniunea Europeană, conform GDPR, ai dreptul:",
        {
          list: [
            "**de acces și la portabilitate** — descarci oricând o copie a tuturor datelor tale (fișier JSON plus fotografiile și imaginile) din [Contul meu → Datele mele](/account?tab=privacy);",
            "**la rectificare** — îți modifici datele din cont și anunțurile oricând;",
            "**la ștergere** — îți ștergi contul din [Contul meu → Datele mele](/account?tab=privacy). Ștergerea este imediată și definitivă. Rămân doar mesajele trimise unor persoane care au încă un cont (fac parte și din corespondența lor; numele tău apare ca „Cont șters”) și raportările făcute de tine;",
            "**la restricționarea prelucrării** și **la opoziție** față de prelucrările bazate pe interesul legitim;",
            "**să-ți retragi consimțământul** sau să oprești notificările oricând, din setările contului sau din linkul de dezabonare din fiecare email;",
            "**să depui o plângere** la Centrul Național pentru Protecția Datelor cu Caracter Personal ([datepersonale.md](https://datepersonale.md)) sau, dacă locuiești în UE, la autoritatea de protecție a datelor din țara ta.",
          ],
        },
        "Pentru orice cerere care nu se poate face direct din cont, scrie-ne la [contact@imova.md](mailto:contact@imova.md). Răspundem în cel mult o lună; în cazuri complexe termenul se poate prelungi cu încă două luni, cu o explicație. Putem cere o confirmare a identității înainte de a da curs cererii.",
      ],
    },
    {
      id: "cookies",
      heading: "Cookie-uri și stocare în browser",
      blocks: [
        "Folosim doar cookie-uri proprii, necesare pentru funcționarea site-ului. Nu folosim cookie-uri de publicitate, de analiză sau de la terți — de aceea nu îți cerem acordul printr-un banner. Dacă vom adăuga vreodată astfel de cookie-uri, îți vom cere întâi acordul.",
        {
          table: {
            columns: ["Nume", "La ce folosește", "Durată"],
            rows: [
              ["imova_token", "Te ține autentificat.", "Până închizi browserul sau, cu „Ține-mă minte”, până la 30 de zile de inactivitate (cel mult 90)."],
              ["imova_refresh", "Reînnoiește autentificarea fără să te mai conectezi.", "La fel ca imova_token."],
              ["imova_vid", "Identificator aleatoriu, anonim, ca o vizualizare sau o afișare a telefonului unui anunț să fie numărată o singură dată pe zi. Nu e legat de identitatea ta și nu e transmis nimănui.", "1 an."],
              ["NEXT_LOCALE", "Ține minte limba site-ului.", "1 an."],
              ["imova_locale_chosen", "Ține minte că ai ales limba manual, ca să nu o schimbăm la autentificare.", "1 an."],
            ],
          },
        },
        "În stocarea browserului păstrăm câteva preferințe și liste, fără date personale: dacă publici ca persoană sau ca agenție (localStorage), ultima căutare pentru butonul „Înapoi” și listele de localități (sessionStorage, șterse la închiderea filei).",
        "Pe paginile de autentificare, butonul „Continuă cu Google” se încarcă de la Google, care poate folosi propriile cookie-uri conform politicii Google. Hărțile se încarcă de la OpenStreetMap, fără cookie-uri.",
        "Poți șterge sau bloca cookie-urile din setările browserului; fără cookie-urile de autentificare nu vei putea rămâne conectat.",
      ],
    },
    {
      id: "security",
      heading: "Cum protejăm datele",
      blocks: [
        "Toată comunicarea cu site-ul este criptată (HTTPS). Parolele sunt stocate doar ca amprente criptografice, nu în clar. Sesiunile expiră și pot fi închise de pe toate dispozitivele; schimbarea parolei închide celelalte sesiuni. Imaginile din mesaje sunt păstrate într-un spațiu privat. Accesul administratorilor este limitat la ce le trebuie pentru moderare, iar acțiunile sensibile sunt înregistrate.",
        "Dacă are loc o încălcare a securității datelor care îți poate afecta drepturile, vom anunța autoritatea și, când legea o cere, și pe tine.",
      ],
    },
    {
      id: "minors",
      heading: "Minori",
      blocks: [
        "Contul IMOVA este pentru persoane de cel puțin 18 ani. Nu colectăm cu bună știință date despre copii; dacă afli că un copil ne-a dat date, scrie-ne și le vom șterge.",
      ],
    },
    {
      id: "changes",
      heading: "Modificări ale acestei politici",
      blocks: [
        "Data ultimei actualizări este afișată în partea de sus a paginii. Dacă schimbăm ceva important, te anunțăm prin email înainte ca modificarea să se aplice.",
      ],
    },
  ],
};
