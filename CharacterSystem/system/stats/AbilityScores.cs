// STR = str;
// DEX = dex;
// CON = con;
// INT = intl;
// WIS = wis;
// CHR = chr;


public class AbilityScores
{
    public int STR { get; set; }
    public int DEX { get; set; }
    public int CON { get; set; }
    public int INT { get; set; }
    public int WIS { get; set; }
    public int CHR { get; set; }

    public AbilityScores(int str, int dex, int con, int intl, int wis, int chr)
    {
        STR = str;
        DEX = dex;
        CON = con;
        INT = intl;
        WIS = wis;
        CHR = chr;
    }
}