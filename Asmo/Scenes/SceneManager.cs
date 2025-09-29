using System;
using System.Collections.Generic;
using Asmo.Gfx;

namespace Asmo.Scenes
{
    /// <summary>
    /// Maintains a stack of scenes, routes update/draw calls, and orchestrates transitions.
    /// </summary>
    public class SceneManager
    {
        private readonly List<SceneEntry> _sceneStack = new();
        private readonly Queue<SceneCommand> _pendingCommands = new();
        private ActiveTransition? _activeTransition;
        private Surface _surface;

        internal SceneManager(Surface surface, SceneServices? services = null)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
            Services = services ?? new SceneServices();
        }

        public SceneManager(Surface surface) : this(surface, null)
        {
        }

        public SceneServices Services { get; }
        public double TotalTime { get; private set; }

        public int SceneCount => _sceneStack.Count;

        /// <summary>
        /// Pushes a new scene on top of the stack immediately.
        /// </summary>
        public void PushScene(IScene scene, ISceneTransition? transition = null)
        {
            QueuePush(scene, transition);
            FlushPendingCommands();
        }

        /// <summary>
        /// Pops the top scene immediately.
        /// </summary>
        public void PopScene(ISceneTransition? transition = null)
        {
            QueuePop(transition);
            FlushPendingCommands();
        }

        /// <summary>
        /// Replaces the active scene with a different scene immediately.
        /// </summary>
        public void ReplaceScene(IScene scene, ISceneTransition? transition = null)
        {
            QueueReplace(scene, transition);
            FlushPendingCommands();
        }

        /// <summary>
        /// Clears the entire scene stack.
        /// </summary>
        public void ClearScenes(ISceneTransition? transition = null)
        {
            QueueClear(transition);
            FlushPendingCommands();
        }

        internal void QueuePush(IScene scene, ISceneTransition? transition)
        {
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));

            _pendingCommands.Enqueue(SceneCommand.Push(scene, transition));
        }

        internal void QueuePop(ISceneTransition? transition)
        {
            _pendingCommands.Enqueue(SceneCommand.Pop(transition));
        }

        internal void QueueReplace(IScene scene, ISceneTransition? transition)
        {
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));

            _pendingCommands.Enqueue(SceneCommand.Replace(scene, transition));
        }

        internal void QueueClear(ISceneTransition? transition)
        {
            _pendingCommands.Enqueue(SceneCommand.Clear(transition));
        }

        /// <summary>
        /// Steps the scene stack and any active transitions.
        /// </summary>
        public void Update(double deltaTime)
        {
            TotalTime += deltaTime;

            FlushPendingCommands();
            if (_sceneStack.Count == 0)
            {
                UpdateTransition(deltaTime);
                FlushPendingCommands();
                return;
            }

            var context = CreateContext(deltaTime);
            foreach (var scene in GetScenesToUpdate())
            {
                scene.Update(context, deltaTime);
            }

            FlushPendingCommands();
            UpdateTransition(deltaTime);
            FlushPendingCommands();
        }

        /// <summary>
        /// Renders the active scenes and any overlaying transition effects.
        /// </summary>
        public void Draw(Surface surface)
        {
            if (surface == null)
                throw new ArgumentNullException(nameof(surface));

            if (!ReferenceEquals(surface, _surface))
            {
                _surface = surface;
            }

            var context = CreateContext(0);
            foreach (var scene in GetScenesToDraw())
            {
                scene.Draw(context, surface);
            }

            if (_activeTransition != null)
            {
                var transitionContext = new SceneTransitionContext(this, surface, 0, _activeTransition.Elapsed, _activeTransition.FromScene, _activeTransition.ToScene);
                _activeTransition.Transition.Draw(transitionContext, surface);
            }
        }

        private SceneContext CreateContext(double deltaTime) => new SceneContext(this, _surface, deltaTime);

        private void FlushPendingCommands()
        {
            while (_pendingCommands.Count > 0)
            {
                // Avoid launching a new transition until the existing one completes.
                if (_activeTransition != null && _pendingCommands.Peek().Transition != null)
                    break;

                var command = _pendingCommands.Dequeue();
                switch (command.Type)
                {
                    case SceneCommandType.Push:
                        ExecutePush(command.Scene!, command.Transition);
                        break;
                    case SceneCommandType.Pop:
                        ExecutePop(command.Transition);
                        break;
                    case SceneCommandType.Replace:
                        ExecuteReplace(command.Scene!, command.Transition);
                        break;
                    case SceneCommandType.Clear:
                        ExecuteClear(command.Transition);
                        break;
                }

                // If the executed command spawned a transition, pause further command processing
                // until the transition finishes to preserve sequencing.
                if (_activeTransition != null)
                    break;
            }
        }

        private void ExecutePush(IScene scene, ISceneTransition? transition)
        {
            var fromScene = _sceneStack.Count > 0 ? _sceneStack[^1].Scene : null;
            _sceneStack.Add(new SceneEntry(scene));
            scene.OnEnter(CreateContext(0));

            if (transition != null)
            {
                StartTransition(transition, fromScene, scene);
            }
        }

        private void ExecutePop(ISceneTransition? transition)
        {
            if (_sceneStack.Count == 0)
                return;

            var fromScene = _sceneStack[^1].Scene;
            fromScene.OnExit(CreateContext(0));
            _sceneStack.RemoveAt(_sceneStack.Count - 1);
            var toScene = _sceneStack.Count > 0 ? _sceneStack[^1].Scene : null;

            if (transition != null)
            {
                StartTransition(transition, fromScene, toScene);
            }
        }

        private void ExecuteReplace(IScene scene, ISceneTransition? transition)
        {
            IScene? fromScene = null;
            if (_sceneStack.Count > 0)
            {
                fromScene = _sceneStack[^1].Scene;
                fromScene.OnExit(CreateContext(0));
                _sceneStack.RemoveAt(_sceneStack.Count - 1);
            }

            _sceneStack.Add(new SceneEntry(scene));
            scene.OnEnter(CreateContext(0));

            if (transition != null)
            {
                StartTransition(transition, fromScene, scene);
            }
        }

        private void ExecuteClear(ISceneTransition? transition)
        {
            if (_sceneStack.Count == 0)
                return;

            IScene? lastScene = _sceneStack[^1].Scene;
            for (int i = _sceneStack.Count - 1; i >= 0; i--)
            {
                _sceneStack[i].Scene.OnExit(CreateContext(0));
            }
            _sceneStack.Clear();

            if (transition != null)
            {
                StartTransition(transition, lastScene, null);
            }
        }

        private void StartTransition(ISceneTransition transition, IScene? fromScene, IScene? toScene)
        {
            var context = new SceneTransitionContext(this, _surface, 0, 0, fromScene, toScene);
            transition.Begin(context);
            _activeTransition = new ActiveTransition(transition, fromScene, toScene);
        }

        private void UpdateTransition(double deltaTime)
        {
            if (_activeTransition == null)
                return;

            _activeTransition.Elapsed += deltaTime;
            var context = new SceneTransitionContext(this, _surface, deltaTime, _activeTransition.Elapsed, _activeTransition.FromScene, _activeTransition.ToScene);
            if (_activeTransition.Transition.Update(context))
            {
                _activeTransition.Transition.Complete(context);
                _activeTransition = null;
            }
        }

        private IEnumerable<IScene> GetScenesToUpdate()
        {
            var list = new List<IScene>();
            bool continueUpdating = true;
            for (int i = _sceneStack.Count - 1; i >= 0 && continueUpdating; i--)
            {
                var scene = _sceneStack[i].Scene;
                list.Add(scene);
                if (scene.BlocksUpdateBelow)
                {
                    continueUpdating = false;
                }
            }

            // Update from top-most scene downward for intuitive input handling.
            return list;
        }

        private IEnumerable<IScene> GetScenesToDraw()
        {
            var stack = new Stack<IScene>();
            bool continueDrawing = true;
            for (int i = _sceneStack.Count - 1; i >= 0 && continueDrawing; i--)
            {
                var scene = _sceneStack[i].Scene;
                stack.Push(scene);
                if (scene.BlocksDrawBelow)
                {
                    continueDrawing = false;
                }
            }

            return stack;
        }

        private readonly struct SceneEntry
        {
            public SceneEntry(IScene scene)
            {
                Scene = scene;
            }

            public IScene Scene { get; }
        }

        private readonly struct SceneCommand
        {
            private SceneCommand(SceneCommandType type, IScene? scene, ISceneTransition? transition)
            {
                Type = type;
                Scene = scene;
                Transition = transition;
            }

            public SceneCommandType Type { get; }
            public IScene? Scene { get; }
            public ISceneTransition? Transition { get; }

            public static SceneCommand Push(IScene scene, ISceneTransition? transition) => new(SceneCommandType.Push, scene, transition);
            public static SceneCommand Pop(ISceneTransition? transition) => new(SceneCommandType.Pop, null, transition);
            public static SceneCommand Replace(IScene scene, ISceneTransition? transition) => new(SceneCommandType.Replace, scene, transition);
            public static SceneCommand Clear(ISceneTransition? transition) => new(SceneCommandType.Clear, null, transition);
        }

        private enum SceneCommandType
        {
            Push,
            Pop,
            Replace,
            Clear
        }

        private sealed class ActiveTransition
        {
            public ActiveTransition(ISceneTransition transition, IScene? fromScene, IScene? toScene)
            {
                Transition = transition;
                FromScene = fromScene;
                ToScene = toScene;
            }

            public ISceneTransition Transition { get; }
            public IScene? FromScene { get; }
            public IScene? ToScene { get; }
            public double Elapsed { get; set; }
        }
    }
}
