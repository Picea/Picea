# Composition

How to combine multiple automata into larger systems.

## The Challenge

Real systems have multiple bounded contexts. An e-commerce system might have:
- **Inventory** automaton (tracks stock levels)
- **Pricing** automaton (manages discounts and pricing rules)
- **Orders** automaton (processes orders)

Each is a separate Automaton. How do they interact?

## Product Composition (Parallel)

Two automata running in parallel, each processing their own events:

```text
(A × B).Transition((stateA, stateB), event) =
    (A.Transition(stateA, event), B.Transition(stateB, event))
```

This is the [product](https://en.wikipedia.org/wiki/Product_(category_theory)) in the category of automata. Both machines process the same event independently.

### Use Case: Independent Concerns

```csharp
// Compose two independent automata into one
var composedRuntime = await AutomatonRuntime<
    ProductAutomaton<Inventory, Pricing>,
    (InventoryState, PricingState),
    SystemEvent,
    (InventoryEffect, PricingEffect),
    Unit>.Start(default, observer, interpreter);
```

## Sum Composition (Routing)

Two automata where events are routed to one or the other:

```text
(A + B).Transition(state, event) =
    event is AEvent ? A.Transition(state.A, event)
    event is BEvent ? B.Transition(state.B, event)
```

This is the [coproduct](https://en.wikipedia.org/wiki/Coproduct) — the events form a tagged union, and the transition routes to the appropriate machine.

### Use Case: Multi-Phase Workflows

```csharp
// An order goes through phases: Draft → Confirmed → Shipped
// Each phase is a separate automaton with different rules
public static (OrderState, OrderEffect) Transition(OrderState state, OrderEvent @event) =>
    state.Phase switch
    {
        Phase.Draft => DraftOrder.Transition(state, @event),
        Phase.Confirmed => ConfirmedOrder.Transition(state, @event),
        Phase.Shipped => ShippedOrder.Transition(state, @event),
        _ => throw new UnreachableException()
    };
```

## Feedback Composition (Loop)

One automaton's effects become another's events:

```text
A produces EffectX → Interpreter converts to B's EventY → B produces EffectZ → ...
```

This is the feedback loop built into the runtime. The Interpreter is the bridge between automata:

```csharp
Interpreter<SystemEffect, SystemEvent> interpreter = effect => effect switch
{
    // Inventory effect triggers order event
    InventoryEffect.StockDepleted(var sku) =>
        Ok([new OrderEvent.ItemUnavailable(sku)]),

    // Order effect triggers inventory event
    OrderEffect.ItemReserved(var sku, var qty) =>
        Ok([new InventoryEvent.Reserved(sku, qty)]),

    _ => Ok([])
};
```

## Composition Through Observer

Observers can bridge automata by dispatching events across runtimes:

```csharp
Observer<OrderState, OrderEvent, OrderEffect> bridge =
    async (state, @event, effect) =>
    {
        if (effect is OrderEffect.ItemReserved(var sku, var qty))
            await inventoryRuntime.Dispatch(new InventoryEvent.Reserved(sku, qty));
        return PipelineResult.Ok;
    };
```

## The Algebra of Composition

| Operation | Symbol | C# Pattern | When To Use |
| --------- | ------ | ----------- | ----------- |
| Product | A × B | Tuple state, shared events | Independent concerns in parallel |
| Sum | A + B | Tagged union events, routing | Multi-phase workflows |
| Feedback | A → B → A | Interpreter bridges | Cross-domain interactions |
| Observer bridge | A → B | Observer dispatches to other runtime | Loose coupling between runtimes |

## See Also

- [The Kernel](the-kernel.md) — the base Automaton interface
- [Building Custom Runtimes](../guides/building-custom-runtimes.md) — wiring interpreters for composition
- [Runtimes Compared](runtimes-compared.md) — choosing the right pattern
