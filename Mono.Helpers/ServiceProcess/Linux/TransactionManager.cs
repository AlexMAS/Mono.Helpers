using System.Collections.Generic;
using System.Linq;

namespace System.ServiceProcess.Linux
{
    internal sealed class TransactionManager<TContext>
    {
        public TransactionManager(LinuxServiceLogWriter logWriter)
        {
            _logWriter = logWriter;
            _stages = new List<StageInfo>();
        }


        private readonly LinuxServiceLogWriter _logWriter;
        private readonly List<StageInfo> _stages;


        public TransactionManager<TContext> Stage(string name, Action<TContext> execute, Action<TContext> rollback = null)
        {
            _stages.Add(new StageInfo(name, execute, rollback));

            return this;
        }


        public void Execute(TContext context)
        {
            var rollbackPath = new Stack<StageInfo>();

            foreach (var stage in _stages)
            {
                rollbackPath.Push(stage);

                _logWriter.InfoFormat(Properties.Resources.ExecutingStageIsStarted, stage);

                try
                {
                    stage.Execute(context);

                    _logWriter.InfoFormat(Properties.Resources.ExecutingStageIsSuccessfullyCompleted, stage);
                }
                catch (Exception error)
                {
                    _logWriter.ErrorFormat(Properties.Resources.ExecutingStageIsCompletedWithErrors, stage, error);

                    var rollbackErrors = Rollback(context, rollbackPath);

                    throw new AggregateException(Properties.Resources.ExecutingTransactionFailed, new[] { error }.Concat(rollbackErrors));
                }
            }
        }


        public void Rollback(TContext context)
        {
            var rollbackErrors = Rollback(context, Enumerable.Reverse(_stages));

            if (rollbackErrors.Count > 0)
            {
                throw new AggregateException(Properties.Resources.ExecutingRollbackTransactionFailed, rollbackErrors);
            }
        }


        private List<Exception> Rollback(TContext context, IEnumerable<StageInfo> rollbackPath)
        {
            var errors = new List<Exception>();

            foreach (var stage in rollbackPath)
            {
                _logWriter.InfoFormat(Properties.Resources.RollbackStageIsStarted, stage);

                try
                {
                    stage.Rollback(context);

                    _logWriter.InfoFormat(Properties.Resources.RollbackStageIsSuccessfullyCompleted, stage);
                }
                catch (Exception error)
                {
                    _logWriter.ErrorFormat(Properties.Resources.RollbackStageIsCompletedWithErrors, stage, error);

                    errors.Add(error);
                }
            }

            return errors;
        }


        internal class StageInfo
        {
            public StageInfo(string name, Action<TContext> execute, Action<TContext> rollback)
            {
                _name = name;
                _execute = execute;
                _rollback = rollback;
            }


            private readonly string _name;
            private readonly Action<TContext> _execute;
            private readonly Action<TContext> _rollback;


            public string Name
            {
                get
                {
                    return _name;
                }
            }

            public void Execute(TContext context)
            {
                if (_execute != null)
                {
                    try
                    {
                        _execute(context);
                    }
                    catch (Exception error)
                    {
                        throw new InvalidOperationException(string.Format(Properties.Resources.CantExecuteStage, _name), error);
                    }
                }
            }

            public void Rollback(TContext context)
            {
                if (_rollback != null)
                {
                    try
                    {
                        _rollback(context);
                    }
                    catch (Exception error)
                    {
                        throw new InvalidOperationException(string.Format(Properties.Resources.CantRollbackStage, _name), error);
                    }
                }
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}