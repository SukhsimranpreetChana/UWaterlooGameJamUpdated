using UnityEngine;

public class ScaleSync : MonoBehaviour
{
    private ParticleSystem particles;
    private BoxCollider2D col;
    private AreaEffector2D effector;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Get references to components
        particles = GetComponent<ParticleSystem>();
        col = GetComponent<BoxCollider2D>();
        effector = GetComponent<AreaEffector2D>();

        //Get struct of size and angle for collider
        Vector3 col_size = col.size;
        Vector3 col_angle = transform.eulerAngles;

        var sh = particles.shape;
        var particle_main = particles.main;

        //Perform syncs
        sh.radius = col_size.y / 2;
        sh.position = new Vector3(-col_size.x/2, 0.0f, 0.0f);
        particle_main.startLifetime = col_size.x/10;
        effector.forceAngle = col_angle.z;
    }

}
