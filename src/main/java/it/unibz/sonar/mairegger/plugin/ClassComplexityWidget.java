package it.unibz.sonar.mairegger.plugin;

import org.sonar.api.SonarPlugin;

import java.util.Arrays;
import java.util.List;

@org.sonar.api.web.UserRole(org.sonar.api.web.UserRole.USER)
@org.sonar.api.web.Description("This is the description of the widget")
public class ClassComplexityWidget extends org.sonar.api.web.AbstractRubyTemplate implements org.sonar.api.web.RubyRailsWidget {
    public String getId() {
        return "idemetadata";
    }
    public String getTitle() {
        return "Mairegger Test";
    }
    protected String getTemplatePath() {
        // uncomment next line for change reloading during development
        return "C:/Users/Michael/Documents/git/sonar-unibz/src/main/resources/Complexity.html.erb";
        //return "/xxxxx/sonar/idemetadata/idemetadata_widget.html.erb";
    }
}