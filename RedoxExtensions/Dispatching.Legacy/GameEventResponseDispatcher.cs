using System;
using System.Collections.Generic;
using System.Text;

using RedoxExtensions.Wrapper;

namespace RedoxExtensions.Dispatching.Legacy
{
    /// <summary>
    /// A dispatcher for queueing actions in response to receiving character tells
    /// </summary>
    public class GameEventResponseDispatcher : IDisposable
    {
        private readonly Dictionary<string, Queue<SafeAction>> _chatRecievedResponseQueue = new();

        public GameEventResponseDispatcher()
        {
            REPlugin.Instance.CoreManager.ChatBoxMessage += CoreManager_ChatBoxMessage;
        }

        public void QueueChatResponseAction(string fromCharacter, SafeAction action)
        {
            var loweredCharacterName = fromCharacter.ToLower();
            if (this._chatRecievedResponseQueue.TryGetValue(loweredCharacterName, out var queue))
            {
                queue.Enqueue(action);
            }
            else
            {
                queue = new Queue<SafeAction>();
                this._chatRecievedResponseQueue.Add(loweredCharacterName, queue);
                queue.Enqueue(action);
            }
        }

        public void Dispose()
        {
            REPlugin.Instance.CoreManager.ChatBoxMessage -= CoreManager_ChatBoxMessage;
        }

        private void HandleChatEvent(ChatEvent chatEvent)
        {
            var loweredCharacterName = chatEvent.SourceName.ToLower();
            if (this._chatRecievedResponseQueue.TryGetValue(loweredCharacterName, out var queue))
            {
                var action = queue.Dequeue();
                action();
            }
        }

        void CoreManager_ChatBoxMessage(object sender, Decal.Adapter.ChatTextInterceptEventArgs e)
        {
            REPlugin.Instance.InvokeOperationSafely(() =>
            {
                if (ChatEvent.TryParse(e.Text, out var chatEvent))
                {
                    this.HandleChatEvent(chatEvent);
                }
            });
        }
    }
}
