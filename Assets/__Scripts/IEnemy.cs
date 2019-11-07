using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnemy {

	Vector3 pos { get; set; }
    float touchDamage { get; set; }
    string typeString { get; set; }
    GameObject gameObject { get; }
    Transform transform { get; }
}
