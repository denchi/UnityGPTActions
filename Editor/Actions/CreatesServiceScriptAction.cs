namespace GPTUnity.Actions
{

    [GPTAction(@"Creates a Service Script.
Example:

namespace strange.Game.Services
{
    public interface IGameService
    {
        void Init();    
        void Release();    
    }
    
    public class CommonGameService : IGameService
    {
        #region IGameService Implementation
        
        public void Init()
        {
            
        }

        public void Release()
        {
            
        }
        
        #endregion
    }
}

The IGameService is the base class that all services inherit from.")]
    public class CreatesServiceScriptAction : CreateFileActionBase
    {
        [GPTParameter("Generated Script Code")]
        public string ScriptCode { get; set; }

        public override string Content => ScriptCode;
    }
}