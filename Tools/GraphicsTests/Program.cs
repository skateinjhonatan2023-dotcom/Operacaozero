using System;

static void Check(bool value, string message)
{
    if (!value) throw new Exception(message);
}

var budget = new OZGraphicsBudget();
float previous = 0;
for (int level = 0; level < 4; level++)
{
    budget.Reset(level);
    var profile = budget.Settings;
    Check(budget.Scale == profile.Maximum && profile.Maximum > previous, "Ordem dos perfis");
    previous = profile.Maximum;
    foreach (int fps in new[] { 30, 60, 90 })
    {
        budget.Reset(level);
        for (int i = 0; i < fps * 25; i++) budget.Sample(1.6f / fps, fps);
        Check(budget.Scale == profile.Minimum, "Limite mínimo sob carga");
        for (int i = 0; i < fps * 12; i++) budget.Sample(1.10f / fps, fps);
        Check(budget.Scale == profile.Minimum, "Estabilidade na margem");
        for (int i = 0; i < fps * 85; i++) budget.Sample(1f / fps, fps);
        Check(budget.Scale == profile.Maximum, "Recuperação até o teto do perfil");
        budget.Sample(1.6f / fps, fps);
        Check(!budget.Sample(5, fps) && budget.Scale == profile.Maximum, "Retorno de pausa");
    }
}
budget.Reset(-1); Check(budget.Scale == .8f, "Preferência antiga negativa");
budget.Reset(99); Check(budget.Scale == 1.08f, "Preferência fora do limite");
Console.WriteLine("PASS quatro perfis × três metas, limites, recuperação, estabilidade e pausa.");
