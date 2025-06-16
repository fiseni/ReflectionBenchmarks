namespace ReflectionBenchmarks;

[MemoryDiagnoser]
public class Benchmark0_Include
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
        var result = _queryable
            .Include(x => x.Company)
            .ThenInclude(x => x.Country);

        return result;
    }

    [Benchmark]
    public object Custom1()
    {
        var result = _queryable
            .IncludeCustom1(x => x.Company)
            .ThenIncludeCustom1<Store, Company, Country>(x => x.Country);

        return result;
    }

    [Benchmark]
    public object Custom2()
    {
        var result = _queryable
            .IncludeCustom2(x => x.Company)
            .ThenIncludeCustom2<Store, Company, Country>(x => x.Country, typeof(Company));

        return result;
    }

    //[Benchmark]
    //public object Custom3()
    //{
    //    var result = _queryable
    //        .IncludeCustom3(x => x.Company)
    //        .ThenIncludeCustom3<Store, Company, Country>(x => x.Country);

    //    return result;
    //}

    [Benchmark]
    public object Custom4()
    {
        var result = _queryable
            .IncludeCustom4(x => x.Company)
            .ThenIncludeCustom4<Store, Company, Country>(x => x.Country);

        return result;
    }

    public static void PrintOutput()
    {
        var benchmark0 = new Benchmark0_Include();
        benchmark0.Setup();

        var efCoreResult = benchmark0.EFCore();
        var custom1Result = benchmark0.Custom1();
        var custom2Result = benchmark0.Custom2();
        //var custom3Result = benchmark.Custom3();
        var custom4Result = benchmark0.Custom4();

        Console.WriteLine(((IQueryable<Store>)efCoreResult).ToQueryString());
        Console.WriteLine();
        Console.WriteLine(((IQueryable<Store>)custom1Result).ToQueryString());
        Console.WriteLine();
        Console.WriteLine(((IQueryable<Store>)custom2Result).ToQueryString());
        //Console.WriteLine();
        //Console.WriteLine(((IQueryable<Store>)custom3Result).ToQueryString());
        Console.WriteLine();
        Console.WriteLine(((IQueryable<Store>)custom4Result).ToQueryString());
    }
}
