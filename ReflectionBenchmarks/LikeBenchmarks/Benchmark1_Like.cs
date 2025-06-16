namespace ReflectionBenchmarks;

[MemoryDiagnoser]
public class Benchmark1_Like
{
    private DbSet<Store> _queryable = default!;

    [GlobalSetup]
    public void Setup()
    {
        _queryable = new BenchmarkDbContext().Stores;
    }

    [Benchmark(Baseline = true)]
    public object EFCore()
    {
        var nameSearchTerm = "%tore%";

        return _queryable
            .Where(x => EF.Functions.Like(x.Name, nameSearchTerm) || EF.Functions.Like(x.Name, nameSearchTerm))
            .Where(x => EF.Functions.Like(x.Name, nameSearchTerm));
    }

    [Benchmark]
    public object Custom1()
    {
        var nameSearchTerm = "%tore%";

        return _queryable
            .Like(
            [
                new(x => x.Name, nameSearchTerm, 1),
                new(x => x.Name, nameSearchTerm, 1),
                new(x => x.Name, nameSearchTerm, 2)
            ]);
    }

    public static void PrintOutput()
    {
        var benchmark = new Benchmark1_Like();
        benchmark.Setup();

        Console.WriteLine(((IQueryable<Store>)benchmark.EFCore()).ToQueryString());
        Console.WriteLine();
        Console.WriteLine(((IQueryable<Store>)benchmark.Custom1()).ToQueryString());
        Console.WriteLine();
    }
}
