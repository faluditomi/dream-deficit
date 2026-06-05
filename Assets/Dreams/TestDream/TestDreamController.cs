// TODO: each dream will receive it's own folder in teh assets folder, its own scene with its own controller. all the dream mechanics will be contained within one dream controller
//       that at the end callse EndDream()
public class TestDreamController : DreamController
{
    private void Start()
    {
        Invoke(nameof(EndDream), 5f);
    }
}
