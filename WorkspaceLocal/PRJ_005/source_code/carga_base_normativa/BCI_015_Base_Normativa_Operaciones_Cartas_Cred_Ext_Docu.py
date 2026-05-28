# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_015_Base_Normativa_Operaciones_Cartas_Cred_Ext_Docu
# MAGIC **************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC ************************************************************************************************************************
# MAGIC * Nombre: BCI_015_Base_Normativa_Operaciones_Cartas_Cred_Ext_Docu.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor: BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de generar la informacion de las operaciones cartas credito exterior y documentadas
# MAGIC * Documentación: 
# MAGIC ************************************************************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Mantenciones
# MAGIC **************************************************************************
# MAGIC #### Mantención Nro: 
# MAGIC * Autor: <Nombre Autor> (<Empresa del Autor (Bci/Otra)>) - Ing. SW BCI: <Nombre Ing. SW BCI>
# MAGIC * Fecha: <dd/mm/yyyy> 
# MAGIC * Descripción: <Descripción de la mantención>      
# MAGIC ***************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tablas Entrada y Salida
# MAGIC **************************************************************************
# MAGIC #### Tablas Entrada: 
# MAGIC * tmp_d00_deuda_act_ctg
# MAGIC * slv_Parametricas_db.TBU
# MAGIC * tmp_modelo_indiv
# MAGIC ***************************************************************************
# MAGIC #### Tablas Salida: 
# MAGIC * base_archivos_normativos (Activo 42, 43 Tabla 89 Manual CMF)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variable

# COMMAND ----------

# MAGIC %md
# MAGIC ### Crear Widgets para Capturar de Variables

# COMMAND ----------

dbutils.widgets.removeAll()

dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso:")
dbutils.widgets.text("db_platinumW","","02 platinum DB:")
dbutils.widgets.text("db_plat_tempW","","03 platinum temp db:")
dbutils.widgets.text("db_RiesgoCredW","","04 slv_RiesgoCred_RiesgoCredPer DB:")
dbutils.widgets.text("db_slv_ParametricasW","","05 Parametricas DB:")

dbutils.widgets.text("db_location_plat_tempW","","06 platinum temp db:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

db_platinumX = dbutils.widgets.get("db_platinumW")
spark.conf.set("bci.db_platinumX", db_platinumX)

db_plat_tempX = dbutils.widgets.get("db_plat_tempW")
spark.conf.set("bci.db_plat_tempX", db_plat_tempX)

db_RiesgoCredX = dbutils.widgets.get("db_RiesgoCredW")
spark.conf.set("bci.db_RiesgoCredX", db_RiesgoCredX)

db_slv_ParametricasX = dbutils.widgets.get("db_slv_ParametricasW")
spark.conf.set("bci.db_slv_ParametricasX", db_slv_ParametricasX)


db_location_plat_tempX = dbutils.widgets.get("db_location_plat_tempW")
spark.conf.set("bci.db_location_plat_tempX", db_location_plat_tempX)


print("Parámetros")
print("fechaProcesoX: " + fechaProcesoX)
print("db_platinumX: " + db_platinumX)
print("db_plat_tempX: " + db_plat_tempX)
print("db_RiesgoCredX: " + db_RiesgoCredX)
print("db_slv_ParametricasX: " + db_slv_ParametricasX)
print("db_location_plat_tempX: " + db_location_plat_tempX)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Carga de funciones

# COMMAND ----------

# MAGIC %run "../../Funciones"

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validación de ingreso de parámetros

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Parametro Vacio

# COMMAND ----------

valida_param_vacio(fechaProcesoX,'fechaProcesoX')
valida_param_vacio(db_platinumX,'db_platinumX')
valida_param_vacio(db_plat_tempX,'db_plat_tempX')
valida_param_vacio(db_RiesgoCredX,'db_RiesgoCredX')
valida_param_vacio(db_slv_ParametricasX,'db_slv_ParametricasX')
valida_param_vacio(db_location_plat_tempX,'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Base de Datos

# COMMAND ----------

valida_bd(db_platinumX, 'db_platinumX')
valida_bd(db_plat_tempX, 'db_plat_tempX')
valida_bd(db_RiesgoCredX, 'db_RiesgoCredX')
valida_bd(db_slv_ParametricasX, 'db_slv_ParametricasX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Valida

# COMMAND ----------

valida_fecha_valida(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Futura

# COMMAND ----------

valida_fecha_futura(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Ruta 

# COMMAND ----------

valida_ruta(db_location_plat_tempX, 'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignacion Variables de fecha

# COMMAND ----------

ano = str(fechaProcesoX)[:4]
mes = str(fechaProcesoX)[4:][:2]
dia = str(fechaProcesoX)[6:][:2]
fechanormativo = str(ano+'-'+mes+'-'+dia)
fechacinta = str(dia+'-'+mes+'-'+ano)
anomes = str(ano+mes)
anomesdia = str(ano+mes+dia)
ano_mes_dia = str(ano+'-'+mes+'-01')

print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)
print("fecha_Formato8: " + ano_mes_dia)

# COMMAND ----------



# COMMAND ----------

# MAGIC %md
# MAGIC ## Inicio de Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ### Creacion Tablas Temporales

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci15_basenorma_tmp_cartas_cred_ext

# COMMAND ----------

drop_cartas_cred_ext = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_cred_ext"""

# COMMAND ----------

sql_safe(drop_cartas_cred_ext)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci15_basenorma_tmp_cartas_cred_ext", True)

# COMMAND ----------

create_cartas_cred_ext = """CREATE TABLE """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_cred_ext
(
  periodo_id                      INT, 				
  rut_cliente                     STRING, 				
  dv_cliente                      STRING, 				
  cod_ope_original                STRING, 			
  cod_num_operacion               STRING, 		
  cod_tip_cart                    STRING, 				
  cod_tipo_ope                    STRING, 				
  des_producto_ope                STRING, 			
  des_banca_ope                   STRING, 			
  cod_cartera_ope                 STRING,			
  mto_deuda_ope                   DECIMAL(30, 0),	   			
  num_dias_mora_ope               INT,		
  ind_cdet                      	STRING,					
  fec_ingreso_deteriodo_ope       STRING,
  cod_cctb                      	STRING, 					
  fl_ope_reneg                  	STRING,
  tipo_activo               	    INT,
  nombre_activo             	    STRING,
  factor_expo                   	DECIMAL(30, 6),
  tipo_garantia                   STRING,
  monto_garantia                  STRING,
  pvtg                   	        DECIMAL(30, 6),
  pi_metodo_interno               DECIMAL(30, 6),
  pdi_metodo_interno              DECIMAL(30, 6),
  pe_metodo_interno               DECIMAL(30, 6),
  prov_metodo_interno             DECIMAL(30, 0),
  monto_avalado					          DECIMAL(30, 0)
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci15_basenorma_tmp_cartas_cred_ext' """

# COMMAND ----------

sql_safe(create_cartas_cred_ext)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci15_basenorma_tmp_cartas_cred_ext_pi_pdi_pe

# COMMAND ----------

drop_cartas_cred_ext_pi_pdi_pe = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_cred_ext_pi_pdi_pe"""

# COMMAND ----------

sql_safe(drop_cartas_cred_ext_pi_pdi_pe)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci15_basenorma_tmp_cartas_cred_ext_pi_pdi_pe", True)

# COMMAND ----------

create_cartas_cred_ext_pi_pdi_pe = """CREATE TABLE """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_cred_ext_pi_pdi_pe
(
  periodo_id                      INT, 				
  rut_cliente                     STRING, 				
  dv_cliente                      STRING, 				
  cod_ope_original                STRING, 			
  cod_num_operacion               STRING, 		
  cod_tip_cart                    STRING, 				
  cod_tipo_ope                    STRING, 				
  des_producto_ope                STRING, 			
  des_banca_ope                   STRING, 			
  cod_cartera_ope                 STRING,			
  mto_deuda_ope                   DECIMAL(30, 0),	   			
  num_dias_mora_ope               INT,		
  ind_cdet                      	STRING,					
  fec_ingreso_deteriodo_ope       STRING,
  cod_cctb                      	STRING, 					
  fl_ope_reneg                  	STRING,
  tipo_activo               	    INT,
  nombre_activo             	    STRING,
  pi_cli                   	      DECIMAL(30, 6),
  pdi_cli                   	    DECIMAL(30, 6),
  pe_cli                   	      DECIMAL(30, 7),
  provision                   	  DECIMAL(30, 0),
  tipo_cli                   	    STRING,
  exposicion                      DECIMAL(30, 0),
  factor_expo                   	DECIMAL(30, 6),
  tipo_garantia                   STRING,
  monto_garantia                  DECIMAL(30, 0),
  pvtg                   	        DECIMAL(30, 6),
  pi_metodo_interno               DECIMAL(30, 6),
  pdi_metodo_interno              DECIMAL(30, 6),
  pe_metodo_interno               DECIMAL(30, 6),
  prov_metodo_interno             DECIMAL(30, 0),
  monto_avalado					          DECIMAL(30, 0)
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci15_basenorma_tmp_cartas_cred_ext_pi_pdi_pe' """

# COMMAND ----------

sql_safe(create_cartas_cred_ext_pi_pdi_pe)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci15_basenorma_tmp_cartas_cred_docu

# COMMAND ----------

drop_cartas_cred_docu = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_cred_docu"""

# COMMAND ----------

sql_safe(drop_cartas_cred_docu)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci15_basenorma_tmp_cartas_cred_docu", True)

# COMMAND ----------

create_cartas_cred_docu = """CREATE TABLE """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_cred_docu
(
  periodo_id                      INT, 				
  rut_cliente                     STRING, 				
  dv_cliente                      STRING, 				
  cod_ope_original                STRING, 			
  cod_num_operacion               STRING, 		
  cod_tip_cart                    STRING, 				
  cod_tipo_ope                    STRING, 				
  des_producto_ope                STRING, 			
  des_banca_ope                   STRING, 			
  cod_cartera_ope                 STRING,			
  mto_deuda_ope                   DECIMAL(30, 0),	   			
  num_dias_mora_ope               INT,		
  ind_cdet                      	STRING,					
  fec_ingreso_deteriodo_ope       STRING,
  cod_cctb                      	STRING, 					
  fl_ope_reneg                  	STRING,
  tipo_activo               	    INT,
  nombre_activo             	    STRING,
  factor_expo                   	DECIMAL(30, 6)
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci15_basenorma_tmp_cartas_cred_docu' """

# COMMAND ----------

sql_safe(create_cartas_cred_docu)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci15_basenorma_tmp_cartas_docu_metodo_int

# COMMAND ----------

drop_cartas_docu_metodo_int = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_docu_metodo_int"""

# COMMAND ----------

sql_safe(drop_cartas_docu_metodo_int)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci15_basenorma_tmp_cartas_docu_metodo_int", True)

# COMMAND ----------

create_cartas_docu_metodo_int = """CREATE TABLE """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_docu_metodo_int
(
  periodo_id                      INT, 				
  rut_cliente                     STRING, 				
  dv_cliente                      STRING, 				
  cod_ope_original                STRING, 			
  cod_num_operacion               STRING, 		
  cod_tip_cart                    STRING, 				
  cod_tipo_ope                    STRING, 				
  des_producto_ope                STRING, 			
  des_banca_ope                   STRING, 			
  cod_cartera_ope                 STRING,			
  mto_deuda_ope                   DECIMAL(30, 0),	   			
  num_dias_mora_ope               INT,		
  ind_cdet                      	STRING,					
  fec_ingreso_deteriodo_ope       STRING,
  cod_cctb                      	STRING, 					
  fl_ope_reneg                  	STRING,
  tipo_activo               	    INT,
  nombre_activo             	    STRING,
  pi_cli                   	      STRING,
  pdi_cli                   	    STRING,
  pe_cli                   	      STRING,
  provision                   	  STRING,
  factor_expo                   	DECIMAL(30, 6),
  tipo_garantia                   STRING,
  monto_garantia                  STRING,
  pvtg                   	        DECIMAL(30, 6),
  pi_metodo_interno               DECIMAL(30, 6),
  pdi_metodo_interno              DECIMAL(30, 6),
  pe_metodo_interno               DECIMAL(30, 6),
  prov_metodo_interno             DECIMAL(30, 0),
  monto_avalado					          DECIMAL(30, 0),
  calificacion                    STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci15_basenorma_tmp_cartas_docu_metodo_int' """

# COMMAND ----------

sql_safe(create_cartas_docu_metodo_int)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci15_basenorma_tmp_cartas_docu_not_nulos

# COMMAND ----------

drop_cartas_docu_not_nulos = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_docu_not_nulos"""

# COMMAND ----------

sql_safe(drop_cartas_docu_not_nulos)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci15_basenorma_tmp_cartas_docu_not_nulos", True)

# COMMAND ----------

create_cartas_docu_not_nulos = """CREATE TABLE """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_docu_not_nulos
(
  periodo_id                      INT, 				
  rut_cliente                     STRING, 				
  dv_cliente                      STRING, 				
  cod_ope_original                STRING, 			
  cod_num_operacion               STRING, 		
  cod_tip_cart                    STRING, 				
  cod_tipo_ope                    STRING, 				
  des_producto_ope                STRING, 			
  des_banca_ope                   STRING, 			
  cod_cartera_ope                 STRING,			
  mto_deuda_ope                   DECIMAL(30, 0),	   			
  num_dias_mora_ope               INT,		
  ind_cdet                      	STRING,					
  fec_ingreso_deteriodo_ope       STRING,
  cod_cctb                      	STRING, 					
  fl_ope_reneg                  	STRING,
  tipo_activo               	    INT,
  nombre_activo             	    STRING,
  pi_cli                   	      DECIMAL(30, 6),
  pdi_cli                   	    DECIMAL(30, 6),
  pe_cli                   	      DECIMAL(30, 7),
  provision                   	  DECIMAL(30, 0),
  tipo_cli                   	    STRING,
  exposicion                      DECIMAL(30, 0),
  factor_expo                   	DECIMAL(30, 6),
  tipo_garantia                   STRING,
  monto_garantia                  DECIMAL(30, 0),
  pvtg                   	        DECIMAL(30, 6),
  pi_metodo_interno               DECIMAL(30, 6),
  pdi_metodo_interno              DECIMAL(30, 6),
  pe_metodo_interno               DECIMAL(30, 6),
  prov_metodo_interno             DECIMAL(30, 0),
  monto_avalado					          DECIMAL(30, 0)
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci15_basenorma_tmp_cartas_docu_not_nulos' """

# COMMAND ----------

sql_safe(create_cartas_docu_not_nulos)

# COMMAND ----------

# MAGIC %md
# MAGIC ####Tabla tmp_bci15_modelo_ind_agrp

# COMMAND ----------

drop_tmp_15_modelo_ind_agrp = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_bci15_modelo_ind_agrp """

# COMMAND ----------

sql_safe(drop_tmp_15_modelo_ind_agrp)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_bci15_modelo_ind_agrp", True)

# COMMAND ----------

create_tmp_modelo_ind_agrp15 = """CREATE TABLE """ + db_plat_tempX + """.tmp_bci15_modelo_ind_agrp
(
  periodo_id			      INT,
  rut_cliente				    INT,
  dv_cliente            STRING,
  cod_num_operacion		  STRING,	
  cod_tipo_Activo				STRING,
  pct_pi					      DECIMAL(30, 6),
  pct_pdi					      DECIMAL(30, 6),
  pct_pe					      DECIMAL(30, 6),
  mnt_provision			    DECIMAL(30, 6),
  cod_calificacion		  STRING,
  cod_tipo_garantia		  STRING,
  mnt_garantia		      DECIMAL(30, 6)
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_bci15_modelo_ind_agrp' """
 

# COMMAND ----------

sql_safe(create_tmp_modelo_ind_agrp15)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Desarrollo Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 1: Eliminacion Datos Tabla CARTAS CRED  Tipo Activo 42 y 43 para Fecha de Proceso

# COMMAND ----------

# MAGIC %md
# MAGIC Se elimina los datos de la tabla base_normativa_v3_ft correspondiente a la fecha de proceso que se va a insertar posteriormente

# COMMAND ----------

ult_dia_mes = ultimo_dia_habil(db_slv_ParametricasX, anomes)

ult_dia = ult_dia_mes.toPandas()
fec_proceso = ult_dia.iloc[0]["periodo_id"]

ano_proceso = str(fec_proceso)[:4]
mes_proceso = str(fec_proceso)[4:][:2]
dia_proceso = str(fec_proceso)[6:][:2]
date_proceso = str(ano_proceso+'-'+mes_proceso+'-'+dia_proceso)
date_proceso_2 = str(ano_proceso+mes_proceso+dia_proceso)

print("fecha_Formato9: " + date_proceso)
print("fecha_Formato10: " + date_proceso_2)

# COMMAND ----------

delete_base_normativa_v3_ft = """delete from """ + db_platinumX + """.base_archivos_normativos  where periodo_id = """ + anomes + """ and cod_tabla_89 in (42, 43) """

# COMMAND ----------

sql_safe(delete_base_normativa_v3_ft)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 2 Obtencion Univ Operaciones Cartas Credito Exterior

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones con cartas creditos exteriores desde tmp_d00_deuda_act_ctg y TBU
# MAGIC
# MAGIC **Créditos Contingentes**
# MAGIC - **Cuenta IFRS Comienza en 83120**:	Cartas de crédito de operaciones de circulación de mercancías
# MAGIC - **Cuenta Contable Comienza en 1620**: Deudores por Cartas de crédito del exterior confirmadas y Deudores por créditos del exterior negociable a plazo   Otros Paises
# MAGIC
# MAGIC
# MAGIC **Tabla de salida**: bci15_basenorma_tmp_cartas_cred_ext (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **tipo_activo**: Identificador del tipo activos
# MAGIC - **nombre_activo**: Nombre del tipo activo
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **tipo_garantia**: Codigo del tipo de garantia cliente
# MAGIC - **monto_garantia**: Monto de garantia cliente
# MAGIC - **pvtg**: Porcentaje PVTG
# MAGIC - **pi_metodo_interno**: Porcentaje probabilidad de incumplimiento interno
# MAGIC - **pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento interno
# MAGIC - **pe_metodo_interno**: Porcentaje de perdida esperada interno
# MAGIC - **prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **monto_avalado**: Monto avalado del cliente

# COMMAND ----------

diaHabil = spark.sql("""select max(fecha_informada) as periodo_id  FROM  """+ db_slv_ParametricasX +""".TBU  
where substring(fecha_informada, 1, 6) = """+ anomes +"""  """)

ultimoDiaHabil_tbu = diaHabil.toPandas()
dia_tbu = ultimoDiaHabil_tbu.iloc[0]["periodo_id"]
 
print(dia_tbu)

# COMMAND ----------

vista_cartas_cred_ext = """ insert into """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_cred_ext
  select  distinct
          a.periodo_id, 				
          a.rut_cliente, 				
          a.dv_cliente, 				
          a.cod_ope_original, 			
          a.cod_num_operacion, 		
          a.cod_tip_cart, 				
          a.cod_tipo_ope, 				
          a.des_producto_ope, 			
          a.des_banca_ope, 			
          a.cod_cartera_ope,			
          a.mto_deuda_ope,	   			
          a.num_dias_mora_ope,		
          a.ind_cdet, 					
          a.fec_ingreso_deteriodo_ope, 
          a.cod_cctb, 					
          a.fl_ope_reneg,
          '42'                                      as tipo_activo,
          'CARTAS CRED. EXTERIOR'                   as nombre_activo,
          0.2                                       as factor_expo,
          'No_Tiene'                                as tipo_garantia,
          0.0                                       as monto_garantia,
          0.0                                       as pvtg,
          0.0                                       as pi_metodo_interno,
          0.0                                       as pdi_metodo_interno,
          0.0                                       as pe_metodo_interno,
          0.0                                       as prov_metodo_interno,
          0.0                                       as monto_avalado		
  from """ + db_plat_tempX + """.tmp_d00_deuda_act_ctg a
  inner join """ + db_slv_ParametricasX + """.TBU b
      on trim(a.cod_cctb) = trim(b.cta_ctable)
      and a.periodo_id = substring(b.fecha_informada, 1, 6)
  where a.periodo_id = '""" + anomes + """'
    and substring(trim(b.cta_cmf_mc), 1, 5) =  '83120'
    and substring(trim(b.cta_ctable), 1, 4) = '1620'
    and trim(a.cod_cartera_ope) = 'CTG'
    and trim(a.fl_ope_reneg) <> 'C'
    and b.fecha_informada = """ + str(dia_tbu)+ """
"""

# COMMAND ----------

sql_safe(vista_cartas_cred_ext)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 3: Obtencion Operaciones Modelo Individual

# COMMAND ----------

# MAGIC %md
# MAGIC Se prepara e inserta los datos a la tabla tmp_modelo_indiv generada en el Notebook 02
# MAGIC
# MAGIC **Tabla de salida**: tmp_bci15_modelo_ind_agrp (tabla de salida)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Fecha de cierre AAAAMM
# MAGIC - **rut_cliente**: Rut del cliente
# MAGIC - **dv_cliente**: digito verificador del cliente
# MAGIC - **cod_num_operacion**: Codigo operacion
# MAGIC - **cod_tipo_activo**: Tipo Activo ACT/CTG
# MAGIC - **pct_pi**: Porcentaje de la probabilidad incumplimiento
# MAGIC - **pct_pdi**: Porcentaje de perdida dada por el incumplimiento
# MAGIC - **pct_pe**: Porcentaje de perdida esperada	
# MAGIC - **mnt_provision**: Monto provisional
# MAGIC - **cod_calificacion**: Codigo clasificacion 
# MAGIC - **cod_tipo_garantia**: Tiene o no garantia
# MAGIC - **mnt_garantia**: Monto de la garantia

# COMMAND ----------

qry_modelo_indv = """ INSERT INTO """ + db_plat_tempX + """.tmp_bci15_modelo_ind_agrp 
select distinct a.periodo_id
      ,a.rut_cliente	
      ,a.dv_cliente 				
      ,a.cod_num_operacion
      ,(CASE WHEN trim(cod_tipo_deu)='NCONT' THEN 'ACT' ELSE 'CTG' END) as cod_tipo_Activo
      ,a.pct_pi                            
      ,a.pct_pdi                          
      ,a.pct_pe                            
      ,sum(a.mnt_provision) mnt_provision
      ,a.cod_calificacion                  
      ,a.cod_tipo_garantia                  
      ,sum(a.mnt_garantia) as mnt_garantia
from """ + db_plat_tempX + """.tmp_modelo_indiv a 
 where a.periodo_id= """+anomes+"""
 group by a.periodo_id
      ,a.rut_cliente	
      ,a.dv_cliente 				
      ,a.cod_num_operacion
      ,(CASE WHEN trim(cod_tipo_deu)='NCONT' THEN 'ACT' ELSE 'CTG' END)  
      ,a.pct_pi                            
      ,a.pct_pdi                          
      ,a.pct_pe                                  
      ,a.cod_calificacion                  
      ,a.cod_tipo_garantia
 """

# COMMAND ----------

sql_safe(qry_modelo_indv)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 4: Obtencion PI, PDI y PE Operaciones Cartas Credito Exterior

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones con PI, PDI, PE entre otras desde bci15_basenorma_tmp_cartas_cred_ext y tmp_modelo_indiv
# MAGIC
# MAGIC **Tabla de salida**: bci15_basenorma_tmp_cartas_cred_ext_pi_pdi_pe (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **tipo_activo**: Identificador del tipo activos
# MAGIC - **nombre_activo**: Nombre del tipo activo
# MAGIC - **pi_cli**: Porcentaje probabilidad de incumplimiento cliente
# MAGIC - **pdi_cli**: Porcentaje de perdida dada por el incumplimiento cliente
# MAGIC - **pe_cli**: Porcentaje de perdida esperada cliente
# MAGIC - **provision**: Monto provision cliente
# MAGIC - **tipo_cli**: Codigo tipo de cliente
# MAGIC - **exposicion**: Monto de exposicion
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **tipo_garantia**: Codigo del tipo de garantia cliente
# MAGIC - **monto_garantia**: Monto de garantia cliente
# MAGIC - **pvtg**: Porcentaje PVTG
# MAGIC - **pi_metodo_interno**: Porcentaje probabilidad de incumplimiento interno
# MAGIC - **pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento interno
# MAGIC - **pe_metodo_interno**: Porcentaje de perdida esperada interno
# MAGIC - **prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **monto_avalado**: Monto avalado del cliente

# COMMAND ----------

vista_cartas_cred_ext_pi_pdi_pe = """ insert into """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_cred_ext_pi_pdi_pe
  select  distinct
          a.periodo_id, 				
          a.rut_cliente, 				
          a.dv_cliente, 				
          a.cod_ope_original, 			
          a.cod_num_operacion, 		
          a.cod_tip_cart, 				
          a.cod_tipo_ope, 				
          a.des_producto_ope, 			
          a.des_banca_ope, 			
          a.cod_cartera_ope,			
          a.mto_deuda_ope,	   			
          a.num_dias_mora_ope,		
          a.ind_cdet, 					
          a.fec_ingreso_deteriodo_ope, 
          a.cod_cctb, 					
          a.fl_ope_reneg,
          a.tipo_activo,
          a.nombre_activo,
          b.pct_pi				                as pi_cli,
          b.pct_pdi				                as pdi_cli,
          b.pct_pe				                as pe_cli,
          b.mnt_provision		              as provision,
          b.cod_calificacion              as tipo_cli,
          round(case when trim(a.ind_cdet) = 'N' or (trim(a.ind_cdet) = 'D' and b.pct_pi <> 1) then a.mto_deuda_ope * 0.2
                else 0
          end)                            as exposicion,
          a.factor_expo,
          a.tipo_garantia,
          a.monto_garantia,
          a.pvtg,
          a.pi_metodo_interno,
          a.pdi_metodo_interno,
          a.pe_metodo_interno,
          a.prov_metodo_interno,
          a.monto_avalado
from """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_cred_ext a
left join  """ + db_plat_tempX + """.tmp_bci15_modelo_ind_agrp  b
     on a.periodo_id = b.periodo_id
     and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
     and trim(a.cod_cartera_ope) = trim(b.cod_tipo_Activo)
/*left join """ + db_plat_tempX + """.tmp_modelo_indiv b    
     on a.periodo_id = b.periodo_id
     and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
     and trim(a.cod_cartera_ope) = (CASE WHEN trim(b.cod_tipo_deu)='NCONT' THEN 'ACT' ELSE 'CTG' END)  */
where a.periodo_id = """+anomes+"""
"""

# COMMAND ----------

sql_safe(vista_cartas_cred_ext_pi_pdi_pe)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 5: Insercion Registros Tipo Activo 42 Tabla base_normativa_v3_ft

# COMMAND ----------

# MAGIC %md
# MAGIC Se prepara e inserta los datos a la tabla base_normativa_v3_ft desde la vista bci15_basenorma_tmp_cartas_cred_ext_pi_pdi_pe
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v3_ft (tabla de salida)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Codigo tipo activo
# MAGIC - **nom_activo**: Nombre del tipo activo
# MAGIC - **pct_pi**: Porcentaje de la probabilidad incumplimiento
# MAGIC - **pct_pdi**: Porcentaje de perdida dada por el incumplimiento
# MAGIC - **pct_pe**: Porcentaje de perdida esperada
# MAGIC - **mnt_provision**: Monto provisional
# MAGIC - **cod_calif**: Codigo tipo clasificacion cliente
# MAGIC - **mnt_provision**: Monto de exposicion
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **des_tipo_gtia**: Tiene o no garantia
# MAGIC - **mnt_garantia**: Monto de la garantia
# MAGIC - **pct_ltv**: Porcentaje PVTG
# MAGIC - **pct_pi_metodo_interno**: Porcentaje probabilidad de incumplimiento metodo interno
# MAGIC - **pct_pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento metodo interno
# MAGIC - **pct_pe_metodo_interno**: Porcentaje de perdida esperada metodo interno
# MAGIC - **mnt_prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **mnt_avalado**: Monto avalado del cliente	
# MAGIC - **fec_proceso**: Fecha de cierre

# COMMAND ----------

insert_base_normativa_v3_ft = """ insert into """ + db_platinumX + """.base_archivos_normativos
  select  periodo_id, 				
          rut_cliente, 				
          dv_cliente, 				
          cod_ope_original, 			
          cod_num_operacion, 		
          cod_tip_cart, 				
          cod_tipo_ope, 				
          des_producto_ope, 			
          des_banca_ope, 			
          cod_cartera_ope,			
          mto_deuda_ope,	   			
          num_dias_mora_ope,		
          ind_cdet, 					
          fec_ingreso_deteriodo_ope, 
          cod_cctb, 					
          fl_ope_reneg,
          -998  as cod_tabla_34,
          tipo_activo					              as cod_tipo_activo,
          nombre_activo				              as nom_activo,
          pi_cli								            as pct_pi,
          pdi_cli								            as pct_pdi,
          pe_cli								            as pct_pe,
          provision						              as mnt_provision,
          tipo_cli					                as cod_calif,
          exposicion						            as mnt_provision,
          factor_expo,
          tipo_garantia                     as des_tipo_gtia,
          monto_garantia                    as mnt_garantia,
          pvtg							                as pct_ltv,
          pi_metodo_interno				          as pct_pi_metodo_interno,
          pdi_metodo_interno				        as pct_pdi_metodo_interno,
          pe_metodo_interno				          as pct_pe_metodo_interno,
          prov_metodo_interno				        as mnt_prov_metodo_interno,
          monto_avalado					            as mnt_avalado,			     
          '""" + date_proceso + """'        as fec_proceso
  from """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_cred_ext_pi_pdi_pe
"""

# COMMAND ----------

sql_safe(insert_base_normativa_v3_ft)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 6: Obtencion Univ Operaciones Cartas Credito Documentadas

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones con cartas creditos documentadas desde tmp_d00_deuda_act_ctg y TBU
# MAGIC
# MAGIC **Créditos Contingentes**
# MAGIC - **Cuenta IFRS Comienza en 83120**:	Cartas de crédito de operaciones de circulación de mercancías
# MAGIC - **Cuenta Contable Comienza en 1615**: Vista, Plazo, Deudores por Cartas de Crédito emitidas terceros países (Vista) , IPC Deudores por C.C. - vista y IPC Deudores por C.C. - plazo
# MAGIC
# MAGIC **Tabla de salida**: bci15_basenorma_tmp_cartas_cred_docu (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **tipo_activo**: Identificador del tipo activos
# MAGIC - **nombre_activo**: Nombre del tipo activo
# MAGIC - **factor_expo**: Porcentaje factor exposicion

# COMMAND ----------

vista_cartas_cred_docu = """ insert into """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_cred_docu
  select  distinct
          a.periodo_id, 				
          a.rut_cliente, 				
          a.dv_cliente, 				
          a.cod_ope_original, 			
          a.cod_num_operacion, 		
          a.cod_tip_cart, 				
          a.cod_tipo_ope, 				
          a.des_producto_ope, 			
          a.des_banca_ope, 			
          a.cod_cartera_ope,			
          a.mto_deuda_ope,	   			
          a.num_dias_mora_ope,		
          a.ind_cdet, 					
          a.fec_ingreso_deteriodo_ope, 
          a.cod_cctb, 					
          a.fl_ope_reneg,
          43                                        as tipo_activo,
          'CARTAS CRED. DOCUMENTADAS'               as nombre_activo,
          0.2                                       as factor_expo	
  from """ + db_plat_tempX + """.tmp_d00_deuda_act_ctg a
  inner join """ + db_slv_ParametricasX + """.TBU b
    on trim(a.cod_cctb) = trim(b.cta_ctable)
      and a.periodo_id = substring(b.fecha_informada, 1, 6)
  where a.periodo_id = '""" + anomes + """'
    and substring(trim(b.cta_cmf_mc), 1, 5) =  '83120'
    and substring(trim(b.cta_ctable), 1, 4) = '1615'
    --and upper(trim(b.rev_tipo)) LIKE '%COMERCIO EXTERIOR%'
    and trim(a.cod_cartera_ope) = 'CTG'
    and trim(a.fl_ope_reneg) <> 'C'
    and b.fecha_informada = """ + str(dia_tbu)+ """
"""

# COMMAND ----------

sql_safe(vista_cartas_cred_docu)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 7: Obtencion PI, PDI y PE Metodo Interno Operaciones Cartas Credito Documentada

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones con PI, PDI, PE metodo interno entre otras desde bci15_basenorma_tmp_cartas_cred_docu y tmp_modelo_com_gr
# MAGIC
# MAGIC **Tabla de salida**: bci15_basenorma_tmp_cartas_docu_metodo_int (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **tipo_activo**: Identificador del tipo activos
# MAGIC - **nombre_activo**: Nombre del tipo activo
# MAGIC - **pi_cli**: Porcentaje probabilidad de incumplimiento cliente
# MAGIC - **pdi_cli**: Porcentaje de perdida dada por el incumplimiento cliente
# MAGIC - **pe_cli**: Porcentaje de perdida esperada cliente
# MAGIC - **provision**: Monto provision cliente
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **tipo_garantia**: Codigo del tipo de garantia cliente
# MAGIC - **monto_garantia**: Monto de garantia cliente
# MAGIC - **pvtg**: Porcentaje PVTG
# MAGIC - **pi_metodo_interno**: Porcentaje probabilidad de incumplimiento interno
# MAGIC - **pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento interno
# MAGIC - **pe_metodo_interno**: Porcentaje de perdida esperada interno
# MAGIC - **prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **monto_avalado**: Monto avalado del cliente

# COMMAND ----------

vista_cartas_docu_ext_metodo_int = """ insert into """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_docu_metodo_int
  select  distinct
          a.periodo_id, 				
          a.rut_cliente, 				
          a.dv_cliente, 				
          a.cod_ope_original, 			
          a.cod_num_operacion, 		
          a.cod_tip_cart, 				
          a.cod_tipo_ope, 				
          a.des_producto_ope, 			
          a.des_banca_ope, 			
          a.cod_cartera_ope,			
          a.mto_deuda_ope,	   			
          a.num_dias_mora_ope,		
          a.ind_cdet, 					
          a.fec_ingreso_deteriodo_ope, 
          a.cod_cctb, 					
          a.fl_ope_reneg,
          a.tipo_activo,
          a.nombre_activo,
          b.pct_pi				                          as pi_cli,
          b.pct_pdi				                          as pdi_cli,
          b.pct_pe				                          as pe_cli,
          b.mnt_prov_oficial		                    as provision,
          a.factor_expo,
          b.des_tipo_gtia                           as tipo_garantia,
          b.mnt_garantia                            as monto_garantia,
          coalesce(b.pct_ltv, 0.0)                  as pvtg,
          coalesce(b.pct_pi_metodo_interno, 0.0)    as pi_metodo_interno,
          coalesce(b.pct_pdi_metodo_interno, 0.0)   as pdi_metodo_interno,
          coalesce(b.pct_pe_metodo_interno, 0.0)    as pe_metodo_interno,
          coalesce(b.mnt_prov_int, 0.0)             as prov_metodo_interno,
          coalesce(b.mnt_avalado, 0.0)              as monto_avalado , 
          case when b.periodo_id is null then 'SIN'
                else 'GR' end                       as calificacion
  from """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_cred_docu a
  left join """ + db_plat_tempX + """.tmp_modelo_com_gr b
      on trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
      and a.periodo_id = b.periodo_id
      and trim(a.cod_cartera_ope) = trim(b.cod_stp_cart)
where a.periodo_id = """+anomes+"""
"""

# COMMAND ----------

sql_safe(vista_cartas_docu_ext_metodo_int)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 8: Obtencion Operaciones Cartas Credito Documentada Sin Nulos

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion de las operaciones cartas credito documentada sin nulos entre otras desde bci15_basenorma_tmp_cartas_docu_metodo_int y tmp_modelo_indiv
# MAGIC
# MAGIC **Tabla de salida**: bci15_basenorma_tmp_cartas_docu_not_nulos (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **tipo_activo**: Identificador del tipo activos
# MAGIC - **nombre_activo**: Nombre del tipo activo
# MAGIC - **pi_cli**: Porcentaje probabilidad de incumplimiento cliente
# MAGIC - **pdi_cli**: Porcentaje de perdida dada por el incumplimiento cliente
# MAGIC - **pe_cli**: Porcentaje de perdida esperada cliente
# MAGIC - **provision**: Monto provision cliente
# MAGIC - **tipo_cli**: Codigo tipo de cliente
# MAGIC - **exposicion**: Monto de exposicion
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **tipo_garantia**: Codigo del tipo de garantia cliente
# MAGIC - **monto_garantia**: Monto de garantia cliente
# MAGIC - **pvtg**: Porcentaje PVTG
# MAGIC - **pi_metodo_interno**: Porcentaje probabilidad de incumplimiento interno
# MAGIC - **pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento interno
# MAGIC - **pe_metodo_interno**: Porcentaje de perdida esperada interno
# MAGIC - **prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **monto_avalado**: Monto avalado del cliente

# COMMAND ----------

vista_cartas_cred_ext_pi_pdi_pe = """ insert into """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_docu_not_nulos
  select  distinct
          a.periodo_id, 				
          a.rut_cliente, 				
          a.dv_cliente, 				
          a.cod_ope_original, 			
          a.cod_num_operacion, 		
          a.cod_tip_cart, 				
          a.cod_tipo_ope, 				
          a.des_producto_ope, 			
          a.des_banca_ope, 			
          a.cod_cartera_ope,			
          a.mto_deuda_ope,	   			
          a.num_dias_mora_ope,		
          a.ind_cdet, 					
          a.fec_ingreso_deteriodo_ope, 
          a.cod_cctb, 					
          a.fl_ope_reneg,
          a.tipo_activo,
          a.nombre_activo,
          coalesce(a.pi_cli, b.pct_pi)				              as pi_cli,
          coalesce(a.pdi_cli, b.pct_pi)				            as pdi_cli,
          coalesce(a.pe_cli, b.pct_pi)			                as pe_cli,
          coalesce(a.provision, b.mnt_provision)		        as provision,
          case when trim(a.calificacion) = 'SIN' then trim(b.cod_calificacion)
            else 'GR'
          end  /*
           case when a.periodo_id is null then trim(b.cod_calificacion)
            else 'GR'
          end   */                                     as tipo_cli,
          round(case when trim(a.ind_cdet) = 'N' 
                        or (trim(a.ind_cdet) = 'D' and coalesce(a.pi_cli, b.pct_pi) <> 1) then a.mto_deuda_ope * 0.2
            else 0
          end)                                              as exposicion,
          a.factor_expo,
          coalesce(a.tipo_garantia, b.cod_tipo_garantia)	  as tipo_garantia,
          coalesce(a.monto_garantia, b.mnt_garantia)		    as monto_garantia,
          a.pvtg,
          a.pi_metodo_interno,
          a.pdi_metodo_interno,
          a.pe_metodo_interno,
          a.prov_metodo_interno,
          a.monto_avalado
  from """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_docu_metodo_int a
  left join """ + db_plat_tempX + """.tmp_modelo_indiv b
      on trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
      and a.periodo_id = b.periodo_id
      and b.mnt_provision <> 0
      and trim(a.cod_cartera_ope) = (CASE WHEN trim(cod_tipo_deu)='NCONT' THEN 'ACT' ELSE 'CTG' END)
where a.periodo_id = """+anomes+"""
"""

# COMMAND ----------

sql_safe(vista_cartas_cred_ext_pi_pdi_pe)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Paso 9: Insercion Registros Tipo Activo 43 Tabla base_normativa_v3_ft

# COMMAND ----------

# MAGIC %md
# MAGIC Se prepara e inserta los datos a la tabla base_normativa_v3_ft desde la vista bci15_basenorma_tmp_cartas_docu_not_nulos
# MAGIC
# MAGIC **Tabla de salida**: base_normativa_v3_ft (tabla de salida)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Periodo de ejecucion AAAAMM 				
# MAGIC - **rut_cliente**: Rut del cliente 				
# MAGIC - **dv_cliente**: Digito verificador del cliente			
# MAGIC - **cod_ope_original**: Operación Original (TRZ) 			
# MAGIC - **cod_num_operacion**: Número Operación 		
# MAGIC - **cod_tip_cart**: Tipo cartera 				
# MAGIC - **cod_tipo_ope**: Tipo-subtipo del D00 				
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera 			
# MAGIC - **des_banca_ope**: Descripción operacion banca  			
# MAGIC - **cod_cartera_ope**: Subtipo cartera			
# MAGIC - **mto_deuda_ope**: Saldo total ifrs	   			
# MAGIC - **num_dias_mora_ope**: Dias de mora		
# MAGIC - **ind_cdet**: Indicador cartera deterioro  					
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación 
# MAGIC - **cod_cctb**: Cuenta contable					
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada
# MAGIC - **cod_tipo_activo**: Codigo tipo activo
# MAGIC - **nom_activo**: Nombre del tipo activo
# MAGIC - **pct_pi**: Porcentaje de la probabilidad incumplimiento
# MAGIC - **pct_pdi**: Porcentaje de perdida dada por el incumplimiento
# MAGIC - **pct_pe**: Porcentaje de perdida esperada
# MAGIC - **mnt_provision**: Monto provisional
# MAGIC - **cod_calif**: Codigo tipo clasificacion cliente
# MAGIC - **mnt_provision**: Monto de exposicion
# MAGIC - **factor_expo**: Porcentaje factor exposicion
# MAGIC - **des_tipo_gtia**: Tiene o no garantia
# MAGIC - **mnt_garantia**: Monto de la garantia
# MAGIC - **pct_ltv**: Porcentaje PVTG
# MAGIC - **pct_pi_metodo_interno**: Porcentaje probabilidad de incumplimiento metodo interno
# MAGIC - **pct_pdi_metodo_interno**: Porcentaje de perdida dada por el incumplimiento metodo interno
# MAGIC - **pct_pe_metodo_interno**: Porcentaje de perdida esperada metodo interno
# MAGIC - **mnt_prov_metodo_interno**: Monto provision metodo interno
# MAGIC - **mnt_avalado**: Monto avalado del cliente	
# MAGIC - **fec_proceso**: Fecha de cierre

# COMMAND ----------

insert_base_normativa_v3_ft_2 = """ insert into """ + db_platinumX + """.base_archivos_normativos
  select  periodo_id, 				
          rut_cliente, 				
          dv_cliente, 				
          cod_ope_original, 			
          cod_num_operacion, 		
          cod_tip_cart, 				
          cod_tipo_ope, 				
          des_producto_ope, 			
          des_banca_ope, 			
          cod_cartera_ope,			
          mto_deuda_ope,	   			
          num_dias_mora_ope,		
          ind_cdet, 					
          fec_ingreso_deteriodo_ope, 
          cod_cctb, 					
          fl_ope_reneg,
          -998                              as cod_tabla_34,
          tipo_activo					              as cod_tipo_activo,
          nombre_activo				              as nom_activo,
          pi_cli								            as pct_pi,
          pdi_cli								            as pct_pdi,
          pe_cli								            as pct_pe,
          provision						              as mnt_provision,
          tipo_cli					                as cod_calif,
          exposicion						            as mnt_provision,
          factor_expo,
          tipo_garantia                     as des_tipo_gtia,
          monto_garantia                    as mnt_garantia,
          pvtg							                as pct_ltv,
          pi_metodo_interno				          as pct_pi_metodo_interno,
          pdi_metodo_interno				        as pct_pdi_metodo_interno,
          pe_metodo_interno				          as pct_pe_metodo_interno,
          prov_metodo_interno				        as mnt_prov_metodo_interno,
          monto_avalado					            as mnt_avalado,			     
          '""" + date_proceso + """'        as fec_proceso
  from """ + db_plat_tempX + """.bci15_basenorma_tmp_cartas_docu_not_nulos
"""

# COMMAND ----------

sql_safe(insert_base_normativa_v3_ft_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Conteo de registros insertados en tabla de salida

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """ + db_platinumX + """.base_archivos_normativos  where periodo_id = """ + anomes + """ and cod_tabla_89 in (42, 43) """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal base_archivos_normativos para el activo 42 y 43")

# COMMAND ----------

# MAGIC %md
# MAGIC # Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")