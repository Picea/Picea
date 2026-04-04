using System.Diagnostics;
using Picea.Commanding;

namespace Picea.Tests;

public sealed class GuardedThermostat
    : GuardedDecider<ThermostatState, ThermostatCommand, ThermostatEvent, ThermostatEffect, Unit>
{
    public static (ThermostatState State, ThermostatEffect Effect) Initialize(Unit parameters) =>
        Thermostat.Initialize(parameters);

    public static ThermostatEvent[] Decide(ThermostatState state, ValidCommand<ThermostatCommand> command) =>
        command.Command switch
        {
            ThermostatCommand.RecordReading(var temp) when temp > Thermostat.AlertThreshold =>
                state.Heating
                    ? [new ThermostatEvent.TemperatureRecorded(temp),
                       new ThermostatEvent.HeaterTurnedOff(),
                       new ThermostatEvent.AlertRaised($"Temperature {temp}°C exceeds alert threshold {Thermostat.AlertThreshold}°C")]
                    : [new ThermostatEvent.TemperatureRecorded(temp),
                       new ThermostatEvent.AlertRaised($"Temperature {temp}°C exceeds alert threshold {Thermostat.AlertThreshold}°C")],

            ThermostatCommand.RecordReading(var temp) when temp < state.TargetTemp && !state.Heating =>
                [new ThermostatEvent.TemperatureRecorded(temp),
                 new ThermostatEvent.HeaterTurnedOn()],

            ThermostatCommand.RecordReading(var temp) when temp >= state.TargetTemp && state.Heating =>
                [new ThermostatEvent.TemperatureRecorded(temp),
                 new ThermostatEvent.HeaterTurnedOff()],

            ThermostatCommand.RecordReading(var temp) =>
                [new ThermostatEvent.TemperatureRecorded(temp)],

            ThermostatCommand.SetTarget(var target) when state.CurrentTemp < target && !state.Heating =>
                [new ThermostatEvent.TargetSet(target),
                 new ThermostatEvent.HeaterTurnedOn()],

            ThermostatCommand.SetTarget(var target) when state.CurrentTemp >= target && state.Heating =>
                [new ThermostatEvent.TargetSet(target),
                 new ThermostatEvent.HeaterTurnedOff()],

            ThermostatCommand.SetTarget(var target) =>
                [new ThermostatEvent.TargetSet(target)],

            ThermostatCommand.Shutdown when state.Heating =>
                [new ThermostatEvent.HeaterTurnedOff(),
                 new ThermostatEvent.ShutdownCompleted()],

            ThermostatCommand.Shutdown =>
                [new ThermostatEvent.ShutdownCompleted()],

            _ => throw new UnreachableException()
        };

    public static (ThermostatState State, ThermostatEffect Effect) Transition(ThermostatState state, ThermostatEvent @event) =>
        Thermostat.Transition(state, @event);

    public static bool IsTerminal(ThermostatState state) => Thermostat.IsTerminal(state);
}

public sealed class ThermostatAuthorizationPolicy
    : GuardedAuthorization<ThermostatPrincipal, ThermostatState, ThermostatCommand, ThermostatError>
{
    private static readonly Policy<ThermostatPrincipal, ThermostatState, ThermostatCommand, ThermostatError> _authorize =
        static (principal, _, command) =>
            (principal.Role, command) switch
            {
                (ThermostatRole.Guest, ThermostatCommand.SetTarget) =>
                    Result<ValidCommand<ThermostatCommand>, ThermostatError>.Err(
                        new ThermostatError.Unauthorized("Guests cannot change target temperature.")),

                (ThermostatRole.Guest, ThermostatCommand.Shutdown) =>
                    Result<ValidCommand<ThermostatCommand>, ThermostatError>.Err(
                        new ThermostatError.Unauthorized("Guests cannot shut down the thermostat.")),

                _ => Result<ValidCommand<ThermostatCommand>, ThermostatError>.Ok(new ValidCommand<ThermostatCommand>(command))
            };

    public static Policy<ThermostatPrincipal, ThermostatState, ThermostatCommand, ThermostatError> Authorize => _authorize;
}

public sealed class ThermostatValidationPolicy
    : GuardedValidation<ThermostatState, ThermostatCommand, ThermostatError>
{
    private static readonly Validator<ThermostatState, ThermostatCommand, ThermostatError> _validate =
        static (state, command) =>
            command switch
            {
                ThermostatCommand.Shutdown when !state.Active =>
                    Result<ValidCommand<ThermostatCommand>, ThermostatError>.Err(
                        new ThermostatError.AlreadyShutdown()),

                _ when !state.Active =>
                    Result<ValidCommand<ThermostatCommand>, ThermostatError>.Err(
                        new ThermostatError.SystemInactive()),

                ThermostatCommand.SetTarget(var target) when target is < Thermostat.MinTarget or > Thermostat.MaxTarget =>
                    Result<ValidCommand<ThermostatCommand>, ThermostatError>.Err(
                        new ThermostatError.InvalidTarget(target, Thermostat.MinTarget, Thermostat.MaxTarget)),

                ThermostatCommand.SetTarget
                    or ThermostatCommand.RecordReading
                    or ThermostatCommand.Shutdown =>
                    Result<ValidCommand<ThermostatCommand>, ThermostatError>.Ok(new ValidCommand<ThermostatCommand>(command)),

                _ => throw new UnreachableException()
            };

    public static Validator<ThermostatState, ThermostatCommand, ThermostatError> Validate => _validate;
}
