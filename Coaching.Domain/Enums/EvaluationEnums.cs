namespace Coaching.Domain.Enums;

public enum EvaluationOutcome
{
    Pending = 0,
    Pass = 1,
    Fail = 2
}

public enum MetricType
{
    Checkbox = 0,   // Yes/No -> 0 or 1
    Slider = 1,     // 1-10 -> value/10
    Number = 2,     // Any number -> value/target
    Ratio = 3       // X/Y -> X/Y
}

public enum VolleyballSkill
{
    Passing = 0,
    Setting = 1,
    Defending = 2,
    Serving = 3,
    Attacking = 4,
    Blocking = 5,
    Game = 6
}

public enum EvaluationSessionStatus
{
    Draft = 0,
    InProgress = 1,
    Completed = 2
}

public enum ParticipantSource
{
    Manual = 0,
    EventParticipant = 1
}
