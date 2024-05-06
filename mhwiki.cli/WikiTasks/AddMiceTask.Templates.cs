﻿namespace mhwiki.cli.WikiTasks;
partial class AddMiceTask
{
    const string MouseGroupCategoryTemplate = """
        Listed below are the {{MouseGroup}}. Further information on these [[mice]] can be found in the [[Effectiveness]] and [[Mouse Group]] articles.

        [[Category:Mice]]
        """;

    const string MousePageTemplate = """
        '''{% raw %}{{{% endraw %}PAGENAME{% raw %}}}{% endraw %}''' is a breed of mouse found on the in [[{{Location}}]].
        {% raw %}{{{% endraw %} Mouse
         | id        = {{ Id }}
         | maxpoints = {{ PointsFormatted }}
         | mingold   = {{ GoldFormatted }}
         | mgroup    = {{ Group }}
         | subgroup  = {{ Subgroup }}
         | habitat   = [[{{Location}}]]
         | loot      = 
         | traptype  = {{ Weaknesses | newline_to_br }}
         | bait      = 
         | charm     = None
         | other     = 
         | mhinfo    = {{ Type }}
         | image     = {% raw %}{{{% endraw %}MHdomain{% raw %}}}{% endraw %}{{ Image }}
         | desc      = {{ Description }}
        {% raw %}}}{% endraw %}

        == Cheese Preference ==
        '''{% raw %}{{{% endraw %}PAGENAME{% raw %}}}{% endraw %}''' is only attracted to .

        == Hunting Strategy ==
        The '''{% raw %}{{{% endraw %}PAGENAME{% raw %}}}{% endraw %}''' can only be attracted ...

        [[CHANGE_ME]] power type is effective against '''{% raw %}{{{% endraw %}PAGENAME{% raw %}}}{% endraw %}'''.<br>
        All other types are ineffective.<br>

        == History and Trivia ==
        *'''{{ReleaseDate}}:''' '''{% raw %}{{{% endraw %}PAGENAME{% raw %}}}{% endraw %}''' was released as part of [[{{Location}}]] location.
        
        """;

    const string RedirectTemplate = """
        #REDIRECT [[{{ To }}]]
        """;
}
