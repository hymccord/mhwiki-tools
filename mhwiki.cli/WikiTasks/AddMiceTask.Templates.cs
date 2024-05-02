namespace mhwiki.cli.WikiTasks;
partial class AddMiceTask
{
    const string MouseGroupCategoryTemplate = """
        Listed below are the {{MouseGroup}}. Further information on these [[mice]] can be found in the [[Effectiveness]] and [[Mouse Group]] articles.

        [[Category:Mice]]
        """;

    const string MouseGroupRedirectTemplate = """
        #REDIRECT [[Mouse Group#{{MouseGroup}}]]
        """;

    const string MousePageTemplate = """
        '''{% raw %}{{{% endraw %}PAGENAME{% raw %}}}{% endraw %}''' is a breed of mouse found on the in [[Placeholder]].
        {% raw %}{{{% endraw %} Mouse
         | id        = {{Id}}
         | maxpoints = {{PointsFormatted}}
         | mingold   = {{GoldFormatted}}
         | mgroup    = {{ Group }}
         | subgroup  = {{ SubGroup }}
         | habitat   = 
         | loot      = 
         | traptype  = 
         | bait      = 
         | charm     = None
         | other     = 
         | mhinfo    = lethargic_guard
         | image     = {% raw %}{{{% endraw %}MHdomain{% raw %}}}{% endraw %}/images/mice/large/{{ImageHash}}.jpg
         | desc      = Guarding all of the inmates of the dungeon is a difficult and tiring task and as this slothful sentry is well aware, the easiest way to deal with a difficult task is... not to do it! The Lethargic Guard is pretty sure that his presence in the dungeon should be enough to keep the prisoners in line and that the "active" part of active duty is largely superfluous.
        {% raw %}}}{% endraw %}

        == Cheese Preference ==
        '''{% raw %}{{{% endraw %}PAGENAME{% raw %}}}{% endraw %}''' is only attracted to .

        == Hunting Strategy ==
        The '''{% raw %}{{{% endraw %}PAGENAME{% raw %}}}{% endraw %}''' can only be attracted after planting a [[Short Vine]] to the [[Dungeon Floor]].

        [[Placeholder]] power type is effective against '''{% raw %}{{{% endraw %}PAGENAME{% raw %}}}{% endraw %}'''.<br>
        All other types are ineffective.<br>

        == History and Trivia ==
        *'''DATE:''' '''{% raw %}{{{% endraw %}PAGENAME{% raw %}}}{% endraw %}''' was released as part of [[Placeholder]] location.
        
        """;
}
