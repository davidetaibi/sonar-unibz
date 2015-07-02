package it.unibz.sonar.mairegger.plugin;

import org.sonar.api.SonarPlugin;

import java.util.Arrays;
import java.util.List;
import org.sonar.api.web.*;

import org.sonar.api.web.WidgetScope;

@WidgetCategory({"Global","Complexity"})
@WidgetScope(WidgetScope.GLOBAL)
@Description("This is the description.")
@UserRole(UserRole.USER)
@WidgetProperties(
{
	@WidgetProperty(
		key="selectedmetric",
		description="This is a mandatory parameter",
		type=WidgetPropertyType.METRIC,
		optional=false
  )
})
public class ClassComplexityWidget extends AbstractRubyTemplate implements RubyRailsWidget {
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