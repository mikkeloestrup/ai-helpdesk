#language: da
Egenskab: Billet status-maskine
    For at sikre korrekt sagsflow
    skal kun gyldige statusovergange være tilladt

Scenarie: Gyldigt fuldt flow gennem en billet
    Givet en ny billet
    Når billetten gennemgår overgangene
        | status         |
        | UnderAiAnalyse |
        | ÅbenUtildelt   |
        | ÅbenTildelt    |
        | AfventerKunde  |
        | Lukket         |
    Så er billettens status "Lukket"

Scenarie: Ugyldig overgang fra Ny afvises
    Givet en ny billet
    Når jeg forsøger at skifte til "Lukket"
    Så afvises overgangen som ugyldig
