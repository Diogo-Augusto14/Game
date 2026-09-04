Ficha razhogor = new Ficha();
Monstro bicho = new Monstro();
Golpe estocada  = new Golpe { nome = "Estocada Baixa", poder = 20, custo = 4 };


foreach (Golpe golpe in razhogor.golpes)
{
    int dano = razhogor.Acerto(golpe.poder , bicho);
    Console.WriteLine(dano);
    
}
foreach (AtaqueMonstro golpem in bicho.ataques)
{
    if (golpem.turnos == 0)
    {
       int levado = razhogor.vida - golpem.poder;
       Console.WriteLine(levado);
    } else
    {
        Console.WriteLine($"buff almentado em  {golpem.turnos}");
    }
}



