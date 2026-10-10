using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;

namespace PaymentsApi.GraphQL;

public static class GraphQLSetup
{
    // Shared by Program.cs and the tests, so the tests run against the exact same schema.
    public static IRequestExecutorBuilder AddPaymentsGraphQL(this IServiceCollection services) =>
        services
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddType<CustomerType>()
            .AddType<InvoiceType>()
            .AddType<PaymentType>()
            .AddType<RefundType>()
            .AddErrorFilter<PaymentErrorFilter>()
            // Hot Chocolate runs sibling fields in parallel by default, but all
            // resolvers in a request share one scoped DbContext, which can't run
            // two queries at once. Serial execution keeps
            // `{ customers { ... } invoices { ... } }` safe.
            .ModifyOptions(options => options.DefaultResolverStrategy = ExecutionStrategy.Serial);
}
